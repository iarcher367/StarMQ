namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Message;
    using Model;
    using System;

    public class ConsumerFactory    // TODO: refactor into DI
    {
        public static IConsumer CreateConsumer(Queue queue, Action<IHandlerRegistrar> configure,
            IConnectionConfiguration configuration, IConnection connection, IOutboundDispatcher dispatcher,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (configure == null)
                throw new ArgumentNullException("configure");

            var log = LogManager.GetLogger(typeof(HandlerManager));

            var handlerManager = new HandlerManager(log);   // TODO: cleanup, DI-compatible?
            configure(handlerManager);
            handlerManager.Validate();

            if (queue.Exclusive)
            {
                log = LogManager.GetLogger(typeof(BasicConsumer));
                return new BasicConsumer(configuration, connection, dispatcher, handlerManager, log,
                    namingStrategy, pipeline, serializationStrategy);
            }
            else
            {
                log = LogManager.GetLogger(typeof(PersistentConsumer));
                return new PersistentConsumer(configuration, connection, dispatcher, handlerManager, log,
                    namingStrategy, pipeline, serializationStrategy);
            }
        }
    }
}
