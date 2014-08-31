namespace StarMQ
{
    using Consume;
    using Core;
    using log4net;
    using Message;
    using Model;
    using Publish;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Queue = Model.Queue;

    public interface IAdvancedBus : IDisposable
    {
        Task ConsumeAsync<T>(Queue queue, Func<T, BaseResponse> messageHandler) where T : class;

        Task ExchangeDeclareAsync(Exchange exchange);

        /// <summary>
        /// With publisher confirms enabled, task completes upon receiving Ack.
        /// Otherwise, it throws an exception on NAck or timeout.           // TODO: verify
        /// </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(Exchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class;

        /// <summary>
        /// Allows a queue to begin receiving messages matching the routing key from specified exchange.
        /// </summary>
        Task QueueBindAsync(Exchange exchange, Queue queue, string routingKey);

        Task QueueDeclareAsync(Queue queue);

        /// <summary>
        /// Fired upon receiving a basic.return for a published message.
        /// </summary>
        event Action BasicReturnEvent;
    }

    public class AdvancedBus : IAdvancedBus
    {
        private const string KeyFormat = "{0}:{1}:{2}";

        private readonly IConnectionConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly IInboundDispatcher _inboundDispatcher;
        private readonly ILog _log;
        private readonly INamingStrategy _namingStrategy;
        private readonly IOutboundDispatcher _outboundDispatcher;
        private readonly IPipeline _pipeline;
        private readonly IPublisher _publisher;
        private readonly ISerializationStrategy _serializationStrategy;
        private readonly ConcurrentDictionary<string, Task> _tasks = new ConcurrentDictionary<string, Task>();

        private bool _disposed;

        public event Action BasicReturnEvent;       // TODO: event to be fired by publisher and re-fired here

        public AdvancedBus(IConnectionConfiguration configuration, IConnection connection,
            IInboundDispatcher inboundDispatcher, ILog log, INamingStrategy namingStrategy, 
            IOutboundDispatcher outboundDispatcher, IPipeline pipeline, IPublisher publisher,
            ISerializationStrategy serializationStrategy)   // TODO: support confirms & basic publishers
        {
            _configuration = configuration;
            _connection = connection;
            _inboundDispatcher = inboundDispatcher;
            _log = log;
            _namingStrategy = namingStrategy;
            _outboundDispatcher = outboundDispatcher;
            _pipeline = pipeline;
            _publisher = publisher;
            _serializationStrategy = serializationStrategy;
        }

        public async Task ConsumeAsync<T>(Queue queue, Func<T, BaseResponse> messageHandler) where T : class
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            // TODO: support derived types on same subscription and multiple handlers

            var consumer = ConsumerFactory.CreateConsumer(queue, _configuration, _connection,
                _inboundDispatcher, _namingStrategy);

            await consumer.Consume(queue, message =>
                {
                    var data = _pipeline.OnReceive(message);
                    var deserialized = _serializationStrategy.Deserialize<T>(data);

                    try
                    {
                        return messageHandler(deserialized.Body);
                    }
                    catch (System.Exception ex)
                    {
                        _log.Error("Unhandled exception from message handler.", ex);

                        return new NackResponse();
                    }
                });

            _log.Info(String.Format("Consumption from queue '{0}' started.", queue.Name));
        }

        public async Task ExchangeDeclareAsync(Exchange exchange)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");

            await _tasks.AddOrUpdate(String.Format(KeyFormat, exchange.Name, String.Empty, String.Empty),
                x => InvokeExchangeDeclareAsync(exchange),
                (_, existing) => existing);
        }

        private async Task InvokeExchangeDeclareAsync(Exchange exchange)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");

            if (exchange.Passive)
            {
                await _outboundDispatcher.Invoke(x => x.ExchangeDeclarePassive(exchange.Name));
            }
            else
            {
                var args = new Dictionary<string, object>();
                var config = new StringBuilder();

                if (!String.IsNullOrEmpty(exchange.AlternateExchangeName))
                {
                    args.Add("alternate-exchange", exchange.AlternateExchangeName);
                    config.Append(" [AE]=").Append(exchange.AlternateExchangeName);
                }

                await _outboundDispatcher.Invoke(x =>
                    x.ExchangeDeclare(exchange.Name, exchange.Type.ToString().ToLower(),
                        exchange.Durable, exchange.AutoDelete, args));

                _log.Info(String.Format("Exchange '{0}' declared.{1}", exchange.Name, config));
            }
        }

        public async Task PublishAsync<T>(Exchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");
            if (message == null)
                throw new ArgumentNullException("message");

            var serialized = _serializationStrategy.Serialize(message);
            var data = _pipeline.OnSend(serialized);

            await _outboundDispatcher.Invoke(x =>
            {
                var properties = x.CreateBasicProperties();
                message.Properties.CopyTo(properties);

                _publisher.Publish(x, a => a.BasicPublish(exchange.Name, routingKey,
                        mandatory, immediate, properties, data.Body));
            });

            _log.Info(String.Format("Message published to '{0}' with routing key '{1}'",
                exchange.Name, routingKey));
        }

        public async Task QueueBindAsync(Exchange exchange, Queue queue, string routingKey)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");

            await _tasks.AddOrUpdate(String.Format(KeyFormat, exchange.Name, queue.Name, routingKey),
                x => InvokeQueueBindAsync(exchange, queue, routingKey),
                (_, existing) => existing);
        }

        private async Task InvokeQueueBindAsync(Exchange exchange, Queue queue, string routingKey)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");

            await _outboundDispatcher.Invoke(x => x.QueueBind(queue.Name, exchange.Name, routingKey));

            _log.Info(String.Format("Queue '{0}' bound to exchange '{1}' with routing key '{2}'.",
                queue.Name, exchange.Name, routingKey));
        }

        public async Task QueueDeclareAsync(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            await _tasks.AddOrUpdate(String.Format(KeyFormat, String.Empty, queue.Name, String.Empty),
                x => InvokeQueueDeclareAsync(queue),
                (_, existing) => existing);
        }

        private async Task InvokeQueueDeclareAsync(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            if (queue.Passive)
            {
                await _outboundDispatcher.Invoke(x => x.QueueDeclarePassive(queue.Name));
            }
            else
            {
                var args = new Dictionary<string, object>();
                var config = new StringBuilder();

                if (!String.IsNullOrEmpty(queue.DeadLetterExchangeName))
                {
                    args.Add("x-dead-letter-exchange", queue.DeadLetterExchangeName);
                    config.Append(" [DLX]=").Append(queue.DeadLetterExchangeName);
                }
                if (!String.IsNullOrEmpty(queue.DeadLetterExchangeRoutingKey))
                    args.Add("x-dead-letter-routing-key", queue.DeadLetterExchangeRoutingKey);
                if (queue.Expiry > 0)
                {
                    args.Add("x-expires", queue.Expiry);
                    config.Append(" [Expiry]=").Append(queue.Expiry);
                }
                if (queue.MessageTimeToLive != uint.MaxValue)
                {
                    args.Add("x-message-ttl", queue.MessageTimeToLive);
                    config.Append(" [TTL]=").Append(queue.MessageTimeToLive);
                }

                await _outboundDispatcher.Invoke(x =>
                    x.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete,
                        args));

                _log.Info(String.Format("Queue '{0}' declared.{1}", queue.Name, config));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _inboundDispatcher.Dispose();
            _outboundDispatcher.Dispose();
            _connection.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}