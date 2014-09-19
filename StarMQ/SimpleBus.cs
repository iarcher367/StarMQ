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
        /// If publisher acknowledgements are enabled, task only completes once publish is confirmed.
        ///
        /// To call synchronously and ensure order, use Wait() to block before publishing again.
        /// </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(T content, string routingKey, bool mandatory = false, bool immediate = false) where T : class;

        /// <summary>
        /// Subscribes to messages of type T matching any routing key.
        /// Sends a nack to the broker for unhandled exceptions and an ack otherwise.
        ///
        /// Subscribers with different subscriptionIds receive copies of each message.
        /// Subscribers with the same subscriptionIds compete for messages.
        /// </summary>
        Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Action<T> messageHandler) where T : class;

        /// <summary>
        /// Subscribes to messages of type T matching any routing key.
        /// Allows custom responses to be sent to the broker.
        ///
        /// Subscribers with different subscriptionIds receive copies of each message.
        /// Subscribers with the same subscriptionIds compete for messages.
        /// </summary>
        Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Func<T, BaseResponse> messageHandler) where T : class;
    }

    public class SimpleBus : ISimpleBus
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly INamingStrategy _namingStrategy;

        public SimpleBus(IAdvancedBus advancedBus, INamingStrategy namingStrategy)
        {
            _advancedBus = advancedBus;
            _namingStrategy = namingStrategy;
        }

        public async Task PublishAsync<T>(T content, string routingKey, bool mandatory = false, bool immediate = false) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");

            var exchange = await ConfigureExchange<T>();

            await _advancedBus.PublishAsync(exchange, routingKey, mandatory, immediate, new Message<T>(content));
        }

        private async Task<Exchange> ConfigureExchange<T>()
        {
            var name = _namingStrategy.GetExchangeName(typeof(T));
            var exchange = new Exchange(name)
            {
                AlternateExchangeName = _namingStrategy.GetAlternateExchangeName(typeof(T)),
                Type = ExchangeType.Topic
            };

            await ConfigureAlternateExchange<T>();
            await _advancedBus.ExchangeDeclareAsync(exchange);

            return exchange;
        }

        private async Task ConfigureAlternateExchange<T>()
        {
            var exchangeName = _namingStrategy.GetAlternateExchangeName(typeof(T));
            var exchange = new Exchange(exchangeName) { Type = ExchangeType.Fanout };
            await _advancedBus.ExchangeDeclareAsync(exchange);

            var queueName = _namingStrategy.GetAlternateQueueName(typeof(T));
            var queue = new Queue(queueName);
            await _advancedBus.QueueDeclareAsync(queue);
            await _advancedBus.QueueBindAsync(exchange, queue, String.Empty);
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

                        return new AckResponse();
                    }
                    catch (System.Exception)
                    {
                        return new NackResponse();
                    }
                });
        }

        public async Task SubscribeAsync<T>(string subscriptionId, List<string> routingKeys, Func<T, BaseResponse> messageHandler) where T : class
        {
            if (subscriptionId == null)
                throw new ArgumentNullException("subscriptionId");
            if (routingKeys == null)
                throw new ArgumentNullException("routingKeys");
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            var exchange = await ConfigureExchange<T>();

            var queueName = _namingStrategy.GetQueueName(typeof(T), subscriptionId);
            var queue = new Queue(queueName)
            {
                DeadLetterExchangeName = _namingStrategy.GetDeadLetterExchangeName(typeof(T))
            };

            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in routingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);

            await ConfigureDeadLettering<T>(subscriptionId, routingKeys);

            await _advancedBus.ConsumeAsync(queue, messageHandler);
        }

        private async Task ConfigureDeadLettering<T>(string subscriptionId, IEnumerable<string> routingKeys)
        {
            var exchangeName = _namingStrategy.GetDeadLetterExchangeName(typeof(T));
            var exchange = new Exchange(exchangeName) { Type = ExchangeType.Topic };
            await _advancedBus.ExchangeDeclareAsync(exchange);

            var queueName = _namingStrategy.GetDeadLetterQueueName(typeof(T), subscriptionId);
            var queue = new Queue(queueName);
            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in routingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);
        }

        public void Dispose()
        {
            _advancedBus.Dispose();
        }
    }
}
