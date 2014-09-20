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
        ///  <summary>
        ///  Publishes a message with given routing key and contents.
        ///  If publisher acknowledgements are enabled, task only completes once publish is confirmed.
        ///
        ///  To call synchronously and ensure order, use Wait() to block before publishing again.
        ///  </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(T content, string routingKey, bool mandatory = false, bool immediate = false, Action<Exchange> configure = null) where T : class;

        /// <summary>
        /// Subscribes to messages of type T matching at least one binding key.
        /// Sends a nack to the broker for unhandled exceptions and an ack otherwise.
        ///
        /// Subscribers to the same queue compete for messages.
        /// Subscribers to different queues with same binding keys receive copies of each message.
        /// </summary>
        Task SubscribeAsync<T>(Action<T> messageHandler, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null) where T : class;

        /// <summary>
        /// Subscribes to messages of type T matching at least one binding key.
        /// Allows custom responses to be sent to the broker.
        ///
        /// Subscribers to the same queue compete for messages.
        /// Subscribers to different queues with same binding keys receive copies of each message.
        /// </summary>
        Task SubscribeAsync<T>(Func<T, BaseResponse> messageHandler, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null) where T : class;
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

        public async Task PublishAsync<T>(T content, string routingKey, bool mandatory = false, bool immediate = false, Action<Exchange> configure = null) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (routingKey == null)
                throw new ArgumentNullException("routingKey");

            var exchange = await SetExchanges<T>(configure);

            await _advancedBus.PublishAsync(exchange, routingKey, mandatory, immediate, new Message<T>(content));
        }

        private async Task<Exchange> SetExchanges<T>(Action<Exchange> configure)
        {
            var exchange = new Exchange { Type = ExchangeType.Topic };

            if (configure != null)
                configure(exchange);

            if (String.IsNullOrEmpty(exchange.Name))
                exchange.WithName(_namingStrategy.GetExchangeName(typeof(T)));
            if (String.IsNullOrEmpty(exchange.AlternateExchangeName))
                exchange.WithAlternateExchangeName(_namingStrategy.GetAlternateName(exchange));

            await SetAlternateExchange(exchange);
            await _advancedBus.ExchangeDeclareAsync(exchange);

            return exchange;
        }

        private async Task SetAlternateExchange(Exchange source)
        {
            var exchange = new Exchange { Type = ExchangeType.Fanout }
                .WithName(source.AlternateExchangeName);
            await _advancedBus.ExchangeDeclareAsync(exchange);

            var queue = new Queue().WithName(source.AlternateExchangeName);
            await _advancedBus.QueueDeclareAsync(queue);
            await _advancedBus.QueueBindAsync(exchange, queue, String.Empty);
        }

        public async Task SubscribeAsync<T>(Action<T> messageHandler, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null) where T : class
        {
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            await SubscribeAsync<T>(x =>
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
                }, configureQueue, configureExchange);
        }

        public async Task SubscribeAsync<T>(Func<T, BaseResponse> messageHandler, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null) where T : class
        {
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            var exchange = await SetExchanges<T>(configureExchange);

            var queue = new Queue();

            if (configureQueue != null)
                configureQueue(queue);

            if (String.IsNullOrEmpty(queue.Name))
                queue.WithName(_namingStrategy.GetQueueName(typeof(T)));
            if (String.IsNullOrEmpty(queue.DeadLetterExchangeName))
                queue.WithDeadLetterExchangeName(_namingStrategy.GetDeadLetterName(exchange.Name));

            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in queue.BindingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);

            await SetDeadLettering(queue, queue.BindingKeys);

            await _advancedBus.ConsumeAsync(queue, messageHandler);
        }

        private async Task SetDeadLettering(Queue source, IEnumerable<string> bindingKeys)
        {
            var exchange = new Exchange { Type = ExchangeType.Topic }
                .WithName(source.DeadLetterExchangeName);
            await _advancedBus.ExchangeDeclareAsync(exchange);

            var queue = new Queue().WithName(_namingStrategy.GetDeadLetterName(source.Name));
            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in bindingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);
        }

        public void Dispose()
        {
            _advancedBus.Dispose();
        }
    }
}
