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
            connection.OnConnected += () => _consumer.Consume(_queue, this);

            _consumer.ConsumerCancelled += (o, e) =>
            {
                var consumerCancelled = ConsumerCancelled;
                if (consumerCancelled != null)
                    consumerCancelled(o, e);

                _consumer.Consume(_queue, this);
            };
        }

        public Task Consume(Queue queue, IBasicConsumer consumer = null)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return _consumer.Consume(queue, this);
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