namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;
    using System;
    using System.Threading.Tasks;

    public class PersistentConsumer : TransientConsumer
    {
        private Queue _queue;

        public PersistentConsumer(IConnection connection, IConsumerDispatcher dispatcher, ILog log,
            INamingStrategy namingStrategy) : base(connection, dispatcher, log, namingStrategy)
        {
            connection.OnConnected += OnConnected;
            connection.OnDisconnected += OnDisconnected;
        }

        public override Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return base.Consume(queue, messageHandler);
        }

        private void OnConnected()
        {
            Consume(_queue);
        }

        private void OnDisconnected()
        {
            Dispatcher.Dispose();
        }
    }
}