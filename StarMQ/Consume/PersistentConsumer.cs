namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Message;
    using Model;
    using System;
    using System.Threading.Tasks;

    public class PersistentConsumer : BasicConsumer
    {
        private Queue _queue;

        public PersistentConsumer(IConnectionConfiguration configuration, IConnection connection,
            IOutboundDispatcher dispatcher, IHandlerManager handlerManager, ILog log,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy,
            ITypeNameSerializer typeNameSerializer)
            : base(configuration, connection, dispatcher, handlerManager, log, namingStrategy, pipeline,
            serializationStrategy, typeNameSerializer)
        {
            Connection.OnConnected += OnConnected;
        }

        private void OnConnected()
        {
            Model = Connection.CreateModel();

            Log.Info("Synchronized model.");

            base.Consume(_queue);
        }

        public override Task Consume(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            _queue = queue;

            return base.Consume(queue);
        }

        public override void HandleBasicCancel(string consumerTag)
        {
            base.HandleBasicCancel(consumerTag);

            base.Consume(_queue);    // TODO: verify new Model is not needed; no need to use OnConnected
        }
    }
}