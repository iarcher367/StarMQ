namespace StarMQ.Subscribe
{
    using Core;
    using log4net;
    using Model;

    public class PersistentConsumer : ConsumerBase
    {
        public PersistentConsumer(Queue queue, IConnection connection, ILog log)
            : base(queue, connection, log)
        {
        }

        public override void Consume()
        {
            throw new System.NotImplementedException();
        }
    }

    public class TransientConsumer : ConsumerBase
    {
        public TransientConsumer(Queue queue, IConnection connection, ILog log)
            : base(queue, connection, log)
        {
        }

        public override void Consume()
        {
            throw new System.NotImplementedException();
        }
    }
}