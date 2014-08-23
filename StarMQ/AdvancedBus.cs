namespace StarMQ
{
    using Core;
    using log4net;
    using Message;
    using Model;
    using Publish;
    using Subscribe;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Queue = Model.Queue;

    public interface IAdvancedBus : IDisposable
    {
        Task ConsumeAsync<T>(Queue queue, Func<T, Response> messageHandler) where T : class;

        Task ExchangeDeclareAsync(Exchange exchange);

        /// <summary>
        /// With publisher confirms enabled, task completes upon receiving Ack.
        /// Otherwise, it throws an exception on NAck or timeout.           // TODO: verify
        /// </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(Exchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class;

        Task QueueDeclareAsync(Queue queue);

        /// <summary>
        /// Fired upon receiving a basic.return for a published message.
        /// </summary>
        event Action BasicReturnEvent;
    }

    public class AdvancedBus : IAdvancedBus
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ConcurrentDictionary<string, Task> _exchanges = new ConcurrentDictionary<string, Task>();
        private readonly ILog _log;
        private readonly INamingStrategy _namingStrategy;
        private readonly IPipeline _pipeline;
        private readonly IPublisher _publisher;
        private readonly ISerializationStrategy _serializationStrategy;

        private bool _disposed;

        public event Action BasicReturnEvent;       // TODO: event to be fired by publisher and re-fired here

        public AdvancedBus(ICommandDispatcher commandDispatcher, ILog log, INamingStrategy namingStrategy,
            IPipeline pipeline, IPublisher publisher, ISerializationStrategy serializationStrategy)
        {
            _commandDispatcher = commandDispatcher;
            _log = log;
            _namingStrategy = namingStrategy;
            _pipeline = pipeline;
            _publisher = publisher;
            _serializationStrategy = serializationStrategy;
        }

        public async Task ConsumeAsync<T>(Queue queue, Func<T, Response> messageHandler) where T : class
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            // TODO: map message type to message handler

            // TODO: null is connection
            var consumer = ConsumerFactory.CreateConsumer(queue, null, _log);

            consumer.Consume();

            //_pipeline.OnReceive(null);

            throw new NotImplementedException();
        }

        public async Task ExchangeDeclareAsync(Exchange exchange)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");

            await _exchanges.AddOrUpdate(exchange.Name,
                x => InvokeExchangeDeclareAsync(exchange),
                (_, existing) => existing);
        }

        private async Task InvokeExchangeDeclareAsync(Exchange exchange)
        {
            if (exchange.Passive)
            {
                await _commandDispatcher.Invoke(x => x.ExchangeDeclarePassive(exchange.Name));
            }
            else
            {
                // TODO: support alternate exchanges

                await _commandDispatcher.Invoke(x =>
                    x.ExchangeDeclare(exchange.Name, exchange.Type.ToString().ToLower(),
                        exchange.Durable, exchange.AutoDelete, null));

                _log.Debug(String.Format("Exchange '{0}' declared.", exchange.Name));
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

            await _commandDispatcher.Invoke(x =>
            {
                var properties = x.CreateBasicProperties();
                message.Properties.CopyTo(properties);

                _publisher.Publish(x, a => a.BasicPublish(exchange.Name, routingKey,
                        mandatory, immediate, properties, data.Body));
            });

            _log.Debug(String.Format("Message published to '{0}' with routing key '{1}'",
                exchange.Name, routingKey));
        }

        public async Task QueueDeclareAsync(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            if (queue.Passive)
            {
                await _commandDispatcher.Invoke(x => x.QueueDeclarePassive(queue.Name));
            }
            else
            {
                // TODO: set TTL, expires

                var args = new Dictionary<string, object>();

                if (!String.IsNullOrEmpty(queue.DeadLetterExchangeName))
                    args.Add("x-dead-letter-exchange", queue.DeadLetterExchangeName);
                if (!String.IsNullOrEmpty(queue.DeadLetterExchangeRoutingKey))
                    args.Add("x-dead-letter-routing-key", queue.DeadLetterExchangeRoutingKey);

                await _commandDispatcher.Invoke(x =>
                    x.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, args));

                _log.Debug(String.Format("Queue '{0}' declared.", queue.Name));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _commandDispatcher.Dispose();

            _log.Info("Disposal complete.");
        }
    }
}