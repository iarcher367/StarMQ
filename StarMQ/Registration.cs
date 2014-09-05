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
            container.RegisterSingle<IConnection, PersistentConnection>();
            container.RegisterSingle<IConnectionConfiguration, ConnectionConfiguration>();
            container.RegisterSingle<IInboundDispatcher, InboundDispatcher>();
            container.RegisterSingle<IOutboundDispatcher, OutboundDispatcher>();
            container.RegisterSingle<IPipeline, InterceptorPipeline>();
            container.RegisterSingle<ISimpleBus, SimpleBus>();

            container.Register(() =>
            {
                var config = container.GetInstance<IConnectionConfiguration>();

                return config.PublisherConfirms
                    ? new ConfirmPublisher(config, LogManager.GetLogger(typeof(ConfirmPublisher)))
                    : (IPublisher)new BasicPublisher(LogManager.GetLogger(typeof(BasicPublisher)));
            });

            container.Register<ICorrelationStrategy, CorrelationStrategy>();
            container.Register<INamingStrategy, NamingStrategy>();
            container.Register<ISerializationStrategy, SerializationStrategy>();
            container.Register<ISerializer, JsonSerializer>();
            container.Register<ITypeNameSerializer, TypeNameSerializer>();

            container.RegisterWithContext(context => LogManager.GetLogger(context.ImplementationType));

            container.Options.AllowOverridingRegistrations = true;

            return container;
        }

        public static void EnableCompression(Container container)      // TODO: refactor to fluent
        {
            var pipeline = container.GetInstance<IPipeline>();
            pipeline.Add(new CompressionInterceptor());
        }
    }
}
