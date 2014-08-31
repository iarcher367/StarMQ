namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;

    public class ConsumerFactory
    {
        public static IConsumer CreateConsumer(Queue queue, IConnectionConfiguration configuration,
            IConnection connection, ILog log, INamingStrategy namingStrategy)
        {
            var dispatcher = new InboundDispatcher(connection, log);   // TODO: refactor into DI container

            return queue.Exclusive
                ? new TransientConsumer(configuration, connection, dispatcher, log, namingStrategy)
                : new PersistentConsumer(configuration, connection, dispatcher, log, namingStrategy);
        }
    }
}
