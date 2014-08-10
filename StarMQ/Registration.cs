namespace StarMQ
{
    using Core;
    using log4net;
    using Message;
    using Publish;
    using SimpleInjector;
    using SimpleInjector.Advanced.Extensions;
    using System;

    public class Registration
    {
        public static void RegisterServices(Container container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            container.RegisterSingle<IAdvancedBus, AdvancedBus>();
            container.RegisterSingle<ICommandDispatcher, CommandDispatcher>();
            container.RegisterSingle<IConnection, PersistentConnection>();
            container.RegisterSingle<IConnectionConfiguration, ConnectionConfiguration>();
            container.RegisterSingle<ISimpleBus, SimpleBus>();

            container.Register<IChannel, PersistentChannel>();
            container.Register(() =>
                {
                    var config = container.GetInstance<IConnectionConfiguration>();
                    var log = LogManager.GetLogger(typeof(IPublisher));        // TODO: inject Log
                    return PublisherFactory.CreatePublisher(config, log);
                });

            container.Register<ICorrelationStrategy, CorrelationStrategy>();
            container.Register<INamingStrategy, NamingStrategy>();
            container.Register<ISerializationStrategy, SerializationStrategy>();
            container.Register<ISerializer, JsonSerializer>();
            container.Register<ITypeNameSerializer, TypeNameSerializer>();

            container.RegisterWithContext(context => LogManager.GetLogger(context.ImplementationType));

            // allows application to use custom implementations
            container.Options.AllowOverridingRegistrations = true;
        }
    }
}
