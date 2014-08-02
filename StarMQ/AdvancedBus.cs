namespace StarMQ
{
    using Core;
    using log4net;
    using Message;
    using System;
    using System.Threading.Tasks;
    using Publish;

    public interface IAdvancedBus : IDisposable
    {
        Task ExchangeDeclareAsync(Exchange exchange);

        /// <summary>
        /// With publisher confirms enabled, task completes upon receiving Ack.
        /// Otherwise, it throws an exception on NAck or timeout.           // TODO: verify
        /// </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">If true, only matching queues with a consumer able to currently accept the message will receive the message. If no matching queue is able to deliver the message, it is returned via basic.return.</param>
        Task PublishAsync<T>(Exchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class;
    }

    public class AdvancedBus : IAdvancedBus
    {
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILog _log;
        private readonly IPublisher _publisher;
        private readonly ISerializationStrategy _serializationStrategy;

        private bool _disposed;

        public AdvancedBus(ICommandDispatcher commandDispatcher, ILog log, IPublisher publisher,
            ISerializationStrategy serializationStrategy)
        {
            _commandDispatcher = commandDispatcher;
            _log = log;
            _publisher = publisher;
            _serializationStrategy = serializationStrategy;
        }

        public async Task ExchangeDeclareAsync(Exchange exchange)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");
            if (exchange.Passive)
                throw new NotImplementedException();

            // TODO: alternate exchange support?

            await _commandDispatcher.Invoke(x =>
                x.ExchangeDeclare(exchange.Name, exchange.Type.ToString().ToLower(),
                    exchange.Durable, exchange.AutoDelete, null));

            _log.Debug(String.Format("Exchange '{0}' declared.", exchange.Name));
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

            await _commandDispatcher.Invoke(x =>
                {
                    var properties = x.CreateBasicProperties();
                    message.Properties.CopyTo(properties);

                    _publisher.Publish(x, a => a.BasicPublish(exchange.Name, routingKey,
                            mandatory, immediate, properties, serialized.Body));
                });

            _log.Debug(String.Format("Message published to '{0}' with routing key '{1}'",
                exchange.Name, routingKey));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _commandDispatcher.Dispose();

            _log.Info("Advanced bus disposed.");
        }
    }
}