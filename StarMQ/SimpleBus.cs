namespace StarMQ
{
    using Core;
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface ISimpleBus : IDisposable
    {
        /// <summary>
        /// Publishes a message with given routing key and contents.
        /// If publisher acknowledgements are enabled, task only completes publish is confirmed.
        ///
        /// To call synchronously and ensure order, use Wait() to block before publishing again.
        /// </summary>
        Task PublishAsync<T>(T content, string routingKey) where T : class;

        /// <summary>
        /// Sends a nack to the broker for unhandled exceptions and an ack otherwise.
        /// </summary>
        Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Action<T> messageHandler) where T : class;

        /// <summary>
        /// Subscribes to messages of type T matching any routing key.
        ///
        /// Subscribers with different subscriptionIds receive copies of each message.
        /// Subscribers with the same subscriptionIds compete for messages.
        ///
        /// Allows custom responses to be sent to the broker.
        /// </summary>
        Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Func<T, Response> messageHandler) where T : class;
    }

    public class SimpleBus : ISimpleBus
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly INamingStrategy _namingStrategy;

        public SimpleBus(IAdvancedBus advancedBus, IConnectionConfiguration configuration,
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

            var name = _namingStrategy.GetExchangeName(typeof(T));
            var exchange = new Exchange(name) { Type = ExchangeType.Topic };

            await _advancedBus.ExchangeDeclareAsync(exchange);
            await _advancedBus.PublishAsync(exchange, routingKey, false, false, new Message<T>(content));
        }

        public async Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Action<T> messageHandler) where T : class
        {
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            await SubscribeAsync<T>(subscriptionId, routingKeys, x =>
                {
                    try
                    {
                        messageHandler(x);

                        return new Response();
                    }
                    catch (System.Exception)
                    {
                        return new Response { Type = ResponseType.Nack };
                    }
                });
        }

        public async Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Func<T, Response> messageHandler) where T : class
        {
            if (subscriptionId == null)
                throw new ArgumentNullException("subscriptionId");
            if (routingKeys == null)
                throw new ArgumentNullException("routingKeys");
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            var exchangeName = _namingStrategy.GetExchangeName(typeof(T));
            var exchange = new Exchange(exchangeName);

            await _advancedBus.ExchangeDeclareAsync(exchange);

            // TODO: support prefetchcount, priority, cancelOnHaFailover?

            var queueName = _namingStrategy.GetQueueName(typeof(T), subscriptionId);
            var queue = new Queue(queueName)
            {
                DeadLetterExchangeName = _namingStrategy.GetDeadLetterExchangeName(typeof(T))
            };

            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in routingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);

            await _advancedBus.ConsumeAsync(queue, messageHandler);
        }

        public void Dispose()
        {
            _advancedBus.Dispose();
        }
    }
}
