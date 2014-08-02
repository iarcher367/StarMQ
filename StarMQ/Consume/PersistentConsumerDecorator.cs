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

namespace StarMQ.Consume
{
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public class PersistentConsumerDecorator : IConsumer
    {
        private readonly IConsumer _consumer;

        private Queue _queue;

        public IModel Model { get { return _consumer.Model; } }

        public event ConsumerCancelledEventHandler ConsumerCancelled;

        public PersistentConsumerDecorator(IConsumer consumer, IConnection connection)
        {
            _consumer = consumer;
            connection.OnConnected += () => _consumer.Consume(_queue, consumer: this);

            _consumer.ConsumerCancelled += (o, e) =>
            {
                var consumerCancelled = ConsumerCancelled;
                if (consumerCancelled != null)
                    consumerCancelled(o, e);

                _consumer.Consume(_queue, consumer: this);
            };
        }

        public Task Consume(Queue queue, Action<IHandlerRegistrar> configure = null, IBasicConsumer consumer = null)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return _consumer.Consume(queue, configure, this);
        }

        #region Pass-through
        public void HandleBasicCancel(string consumerTag)
        {
            _consumer.HandleBasicCancel(consumerTag);
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            _consumer.HandleBasicCancelOk(consumerTag);
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            _consumer.HandleBasicConsumeOk(consumerTag);
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
            string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            _consumer.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey,
                properties, body);
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs reason)
        {
            _consumer.HandleModelShutdown(model, reason);
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
        #endregion
    }
}