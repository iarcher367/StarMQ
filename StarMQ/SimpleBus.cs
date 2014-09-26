namespace StarMQ
{
    using Consume;
    using Core;
    using Model;
    using System;
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
        /// <param name="immediate">Not supported by RabbitMQ - use TTL=0. If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(T content, string routingKey, bool mandatory = false, bool immediate = false, Action<Exchange> configure = null) where T : class;

        /// <summary>
        /// Subscribes to messages matching at least one binding key.
        ///
        /// Subscribers to the same queue compete for messages.
        /// Subscribers to different queues with same binding keys receive copies of each message.
        /// </summary>
        Task SubscribeAsync(Action<IHandlerRegistrar> configureHandlers, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null);
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

            var exchange = await SetExchanges(configure, typeof(T));

            await _advancedBus.PublishAsync(exchange, routingKey, mandatory, immediate, new Message<T>(content));
        }

        private async Task<Exchange> SetExchanges(Action<Exchange> configure, Type type)
        {
            var exchange = new Exchange { Type = ExchangeType.Topic };

            if (configure != null)
                configure(exchange);

            if (String.IsNullOrEmpty(exchange.Name))
                exchange.WithName(_namingStrategy.GetExchangeName(type));
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

        public async Task SubscribeAsync(Action<IHandlerRegistrar> configureHandler, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null)
        {
            if (configureHandler == null)
                throw new ArgumentNullException("configureHandler");

            var transient = new HandlerManager(null);   // TODO: DI refactor
            configureHandler(transient);
            var type = transient.Validate().Default;   // TODO: optimization?

            var exchange = await SetExchanges(configureExchange, type);

            var queue = new Queue();

            if (configureQueue != null)
                configureQueue(queue);

            if (String.IsNullOrEmpty(queue.Name))
                queue.WithName(_namingStrategy.GetQueueName(type));
            if (String.IsNullOrEmpty(queue.DeadLetterExchangeName))
                queue.WithDeadLetterExchangeName(_namingStrategy.GetDeadLetterName(exchange.Name));

            await SetQueue(exchange, queue);

            await SetDeadLettering(queue);

            await _advancedBus.ConsumeAsync(queue, configureHandler);
        }

        private async Task SetQueue(Exchange exchange, Queue queue)
        {
            await _advancedBus.QueueDeclareAsync(queue);

            foreach (var key in queue.BindingKeys.DefaultIfEmpty("#"))
                await _advancedBus.QueueBindAsync(exchange, queue, key);
        }

        private async Task SetDeadLettering(Queue source)
        {
            var exchange = new Exchange { Type = ExchangeType.Topic }
                .WithName(source.DeadLetterExchangeName);
            await _advancedBus.ExchangeDeclareAsync(exchange);

            var queue = new Queue().WithName(_namingStrategy.GetDeadLetterName(source.Name));

            if (String.IsNullOrEmpty(source.DeadLetterRoutingKey))
                foreach(var key in source.BindingKeys)
                    queue.WithBindingKey(key);
            else
                queue.WithBindingKey(source.DeadLetterRoutingKey);

            await SetQueue(exchange, queue);
        }

        public void Dispose()
        {
            _advancedBus.Dispose();
        }
    }
}
