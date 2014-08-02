namespace StarMQ
{
    using Core;
    using Message;
    using System;
    using System.Threading.Tasks;

    public interface IBus : IDisposable
    {
        /// <summary>
        /// Publishes a message with given routing key and contents.
        /// If publisher acknowledgements are enabled, task only completes publish is confirmed.
        /// </summary>
        Task PublishAsync<T>(T content, string routingKey) where T : class;

        /// <summary>
        /// Subscribes to messages of type T.
        /// Subscribers with different subscriptionIds receive copies of each message.
        /// Subscribers with the same subscriptionIds compete for messages.
        /// </summary>
        Task SubscribeAsync<T>(string subscriptionId, Action<T> onMessage) where T : class;
    }

    public class RabbitBus : IBus
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly INamingStrategy _namingStrategy;

        public RabbitBus(IAdvancedBus advancedBus, IConnectionConfiguration configuration,
            INamingStrategy namingStrategy)
        {
            _advancedBus = advancedBus;
            _configuration = configuration;
            _namingStrategy = namingStrategy;
        }

        public async Task PublishAsync<T>(T content, string routingKey) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");

            var name = _namingStrategy.GetExchangeName(typeof(T));     // TODO: pull from config
            var exchange = new Exchange(name) { Type = ExchangeType.Topic };

            await _advancedBus.ExchangeDeclareAsync(exchange);
            await _advancedBus.PublishAsync(exchange, routingKey, false, false, new Message<T>(content));
        }

        public async Task SubscribeAsync<T>(string subscriptionId, Action<T> onMessage) where T : class
        {
            var queueName = _namingStrategy.GetQueueName(typeof(T), subscriptionId);
            var exchangeName = _namingStrategy.GetExchangeName(typeof(T));
            var exchange = new Exchange(exchangeName);

            await _advancedBus.ExchangeDeclareAsync(exchange);

            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _advancedBus.Dispose();
        }
    }
}
