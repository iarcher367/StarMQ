namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;
    using System;
    using System.Threading.Tasks;

    public class PersistentConsumer : BasicConsumer
    {
        private Queue _queue;

        public PersistentConsumer(IConnectionConfiguration configuration, IConnection connection,
            IOutboundDispatcher dispatcher, ILog log, INamingStrategy namingStrategy)
            : base(configuration, connection, dispatcher, log, namingStrategy)
        {
            Connection.OnConnected += OnConnected;
        }

        private void OnConnected()
        {
            Model = Connection.CreateModel();

            Log.Info("Synchronized model.");

            Consume(_queue);
        }

        public override Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return base.Consume(queue, messageHandler);
        }

        public override void HandleBasicCancel(string consumerTag)
        {
            base.HandleBasicCancel(consumerTag);

            Consume(_queue);    // TODO: verify not using OnConnected
        }
    }
}