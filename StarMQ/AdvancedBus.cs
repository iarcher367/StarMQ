#region Apache License v2.0
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
    using log4net;
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
        Task ConsumeAsync(Queue queue, Action<IHandlerRegistrar> configure);

        Task ExchangeDeclareAsync(Exchange exchange);

        /// <summary>
        /// With publisher confirms enabled, dispatcher completes task upon receiving Ack or Nack.
        /// If timeout elapses, message is published again.
        /// </summary>
        /// <param name="mandatory">If true, published messages must be routed at least one queue. Otherwise, returned via basic.return.</param>
        /// <param name="immediate">Not supported by RabbitMQ - use TTL=0. If true, message is only delivered to matching queues with a consumer currently able to accept the message. If no deliveries occur, it is returned via basic.return.</param>
        Task PublishAsync<T>(Exchange exchange, string routingKey, bool mandatory, bool immediate, IMessage<T> message) where T : class;

        /// <summary>
        /// Allows a queue to begin receiving messages matching the routing key from specified exchange.
        /// </summary>
        Task QueueBindAsync(Exchange exchange, Queue queue, string routingKey);

        Task QueueDeclareAsync(Queue queue);

        /// <summary>
        /// Fired upon receiving a basic.return for a published message.
        /// </summary>
        event BasicReturnHandler BasicReturn;
    }

    public class AdvancedBus : IAdvancedBus
    {
        private const string KeyFormat = "{0}:{1}:{2}";

        private readonly IConsumerFactory _consumerFactory;
        private readonly IOutboundDispatcher _dispatcher;
        private readonly ILog _log;
        private readonly IPublisher _publisher;
        private readonly ConcurrentDictionary<string, Task> _tasks = new ConcurrentDictionary<string, Task>();

        private bool _disposed;

        public event BasicReturnHandler BasicReturn;    // TODO: support confirms & basic publishers

        public AdvancedBus(IConsumerFactory consumerFactory, IOutboundDispatcher dispatcher, ILog log,
            IPublisher publisher)
        {
            _consumerFactory = consumerFactory;
            _dispatcher = dispatcher;
            _log = log;
            _publisher = publisher;

            _publisher.BasicReturn += (o, args) =>
            {
                var basicReturn = BasicReturn;
                if (basicReturn != null)
                    BasicReturn(o, args);
            };
        }

        public async Task ConsumeAsync(Queue queue, Action<IHandlerRegistrar> configure)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            await _consumerFactory.CreateConsumer(queue.Exclusive).Consume(queue, configure);

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
                await _dispatcher.Invoke(x => x.ExchangeDeclarePassive(exchange.Name));
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

                await _dispatcher.Invoke(x =>
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

            await _dispatcher.Invoke(() => _publisher.Publish(message,
                (x, y, z) => x.BasicPublish(exchange.Name, routingKey, mandatory, false, y, z)));

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

            await _dispatcher.Invoke(x => x.QueueBind(queue.Name, exchange.Name, routingKey));

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
                await _dispatcher.Invoke(x => x.QueueDeclarePassive(queue.Name));
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
                if (!String.IsNullOrEmpty(queue.DeadLetterRoutingKey))
                    args.Add("x-dead-letter-routing-key", queue.DeadLetterRoutingKey);
                if (queue.Expires > 0)
                {
                    args.Add("x-expires", queue.Expires);
                    config.Append(" [Expires]=").Append(queue.Expires);
                }
                if (queue.MessageTimeToLive != uint.MaxValue)
                {
                    args.Add("x-message-ttl", queue.MessageTimeToLive);
                    config.Append(" [TTL]=").Append(queue.MessageTimeToLive);
                }

                await _dispatcher.Invoke(x =>
                    x.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete,
                        args));

                _log.Info(String.Format("Queue '{0}' declared.{1}", queue.Name, config));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _dispatcher.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}