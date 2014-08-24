namespace StarMQ
{
    using Core;
    using log4net;
    using Message;
    using Publish;
    using SimpleInjector;
    using SimpleInjector.Advanced.Extensions;

    public class Registration
    {
        public static Container RegisterServices()
        {
            var container = new Container();

            container.RegisterSingle<IAdvancedBus, AdvancedBus>();
            container.RegisterSingle<ICommandDispatcher, CommandDispatcher>();
            container.RegisterSingle<IConnection, PersistentConnection>();
            container.RegisterSingle<IConnectionConfiguration, ConnectionConfiguration>();
            container.RegisterSingle<IPipeline, InterceptorPipeline>();
            container.RegisterSingle<ISimpleBus, SimpleBus>();

            container.Register<IChannel, PersistentChannel>();
            container.Register(() =>
                {
                    var config = container.GetInstance<IConnectionConfiguration>();
                    var log = LogManager.GetLogger(config.PublisherConfirms
                        ? typeof(ConfirmPublisher)
                        : typeof(BasicPublisher));
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

            var pipeline = container.GetInstance<IPipeline>();  // TODO: make configuration flexible
            pipeline.Add(new CompressionInterceptor());

            return container;
        }
    }
}
