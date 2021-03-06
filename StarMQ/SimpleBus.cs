﻿#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ
{
    using Consume;
    using Core;
    using Model;
    using Publish;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Publish and subscribe default to Topic exchanges.
    /// </summary>
    public interface ISimpleBus : IDisposable
    {
        ///  <summary>
        ///  Publishes a message with given routing key and contents.
        ///
        ///  To call synchronously and ensure order, use Wait() to block before publishing again.
        ///  </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">Not supported by RabbitMQ - use TTL=0. If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(T content, Action<DeliveryContext> configureContext, bool mandatory = false, bool immediate = false, Action<Exchange> configure = null) where T : class;

        /// <summary>
        /// Subscribes to messages matching at least one binding key.
        ///
        /// Subscribers to the same queue compete for messages.
        /// Subscribers to different queues with same binding keys receive copies of each message.
        /// </summary>
        Task SubscribeAsync(Action<IHandlerRegistrar> configureHandlers, Action<Queue> configureQueue = null, Action<Exchange> configureExchange = null);

        event BasicReturnHandler BasicReturn;
    }

    public class SimpleBus : ISimpleBus
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly INamingStrategy _namingStrategy;

        public event BasicReturnHandler BasicReturn;

        public SimpleBus(IAdvancedBus advancedBus, INamingStrategy namingStrategy)
        {
            _advancedBus = advancedBus;
            _namingStrategy = namingStrategy;

            _advancedBus.BasicReturn += (o, args) =>
            {
                var basicReturn = BasicReturn;
                if (basicReturn != null)
                    BasicReturn(o, args);
            };
        }

        public async Task PublishAsync<T>(T content, Action<DeliveryContext> configureContext, bool mandatory = false, bool immediate = false, Action<Exchange> configure = null) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (configureContext == null)
                throw new ArgumentNullException("configureContext");

            var context = new DeliveryContext();
            configureContext(context);

            var exchange = await SetExchange(configure, typeof(T));

            await SetAlternateExchange(exchange);

            var message = new Message<T>(content) { Properties = context.Properties };

            await _advancedBus.PublishAsync(exchange, context.RoutingKey, mandatory, immediate, message);
        }

        private async Task<Exchange> SetExchange(Action<Exchange> configure, Type type)
        {
            var exchange = new Exchange { Type = ExchangeType.Topic };

            if (configure != null)
                configure(exchange);

            if (String.IsNullOrEmpty(exchange.Name))
                exchange.WithName(_namingStrategy.GetExchangeName(type));
            if (String.IsNullOrEmpty(exchange.AlternateExchangeName))
                exchange.WithAlternateExchangeName(_namingStrategy.GetAlternateName(exchange));

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

            var transient = new HandlerManager(null);
            configureHandler(transient);
            var type = transient.Validate().Default;    // TODO: optimization?

            var exchange = await SetExchange(configureExchange, type);

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
                foreach (var key in source.BindingKeys)
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
