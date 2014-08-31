namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;

    public class ConsumerFactory
    {
        public static IConsumer CreateConsumer(Queue queue, IConnectionConfiguration configuration,
            IConnection connection, IInboundDispatcher dispatcher, INamingStrategy namingStrategy)
        {
            if (queue.Exclusive)
            {
                var log = LogManager.GetLogger(typeof(BasicConsumer));
                return new BasicConsumer(configuration, connection, dispatcher, log, namingStrategy);
            }
            else
            {
                var log = LogManager.GetLogger(typeof(PersistentConsumer));
                return new PersistentConsumer(configuration, connection, dispatcher, log, namingStrategy);
            }
        }
    }
}
