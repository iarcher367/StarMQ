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

        public override Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return base.Consume(queue, messageHandler);
        }

        private void OnConnected()
        {
            Model = Connection.CreateModel();

            Log.Info("Synchronized model.");

            Consume(_queue);
        }
    }
}