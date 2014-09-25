namespace StarMQ
{
    using Core;
    using log4net;
    using Message;
    using Publish;
    using RabbitMQ.Client;
    using SimpleInjector;
    using SimpleInjector.Advanced.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using IConnection = Core.IConnection;

    public class Factory : IDisposable
    {
        internal Container Container { get; private set; }

        public Factory()
        {
            Container = new Container();

            Container.RegisterSingle<IAdvancedBus, AdvancedBus>();
            Container.RegisterSingle<IConnection, PersistentConnection>();
            Container.RegisterSingle<IConnectionConfiguration, ConnectionConfiguration>();
            Container.RegisterSingle<IOutboundDispatcher, OutboundDispatcher>();
            Container.RegisterSingle<IPipeline, InterceptorPipeline>();
            Container.RegisterSingle<ISimpleBus, SimpleBus>();

            Container.Register(() =>
            {
                var config = Container.GetInstance<IConnectionConfiguration>();
                var connection = Container.GetInstance<IConnection>();

                return config.PublisherConfirms
                    ? new ConfirmPublisher(config, connection, LogManager.GetLogger(typeof(ConfirmPublisher)))
                    : (IPublisher)new BasicPublisher(connection, LogManager.GetLogger(typeof(BasicPublisher)));
            });

            Container.Register(() =>
            {
                var apiAssembly = Assembly.GetExecutingAssembly().GetName();
                var config = Container.GetInstance<IConnectionConfiguration>();
                var sourceAssembly = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly())
                    .GetName();

                return new ConnectionFactory
                {
                    HostName = config.Host,
                    Password = config.Password,
                    Port = config.Port,
                    UserName = config.Username,
                    VirtualHost = config.VirtualHost,
                    RequestedHeartbeat = config.Heartbeat,
                    RequestedConnectionTimeout = config.Timeout,
                    ClientProperties = new Dictionary<string, object>
                    {
                        { "clientApi", apiAssembly.Name },
                        { "clientApiVersion", apiAssembly.Version.ToString() },
                        { "machineName", Environment.MachineName },
                        { "platform", ".NET" },
                        { "product", sourceAssembly.Name },
                        { "version", sourceAssembly.Version.ToString() },
                    }
                };
            });

            Container.Register<ICorrelationStrategy, CorrelationStrategy>();
            Container.Register<INamingStrategy, NamingStrategy>();
            Container.Register<ISerializationStrategy, SerializationStrategy>();
            Container.Register<ISerializer, JsonSerializer>();
            Container.Register<ITypeNameSerializer, TypeNameSerializer>();

            Container.RegisterWithContext(context => LogManager.GetLogger(context.ImplementationType));

            Container.Options.AllowOverridingRegistrations = true;
        }

        public Factory AddInterceptor(IMessagingInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException("interceptor");

            Container.GetInstance<IPipeline>().Add(interceptor);
            return this;
        }

        public Factory EnableCompression()
        {
            Container.GetInstance<IPipeline>().Add(new CompressionInterceptor());
            return this;
        }

        /// <param name="secretKey">truncates to 128-bit key</param>
        public Factory EnableEncryption(string secretKey)
        {
            if (secretKey == null)
                throw new ArgumentNullException();
            if (secretKey.Length < 16)
                throw new ArgumentException("Secret key must be 16 characters in length.");

            var key = new JsonSerializer().ToBytes(secretKey.Substring(0, 16));

            Container.GetInstance<IPipeline>().Add(new AesInterceptor(key.Skip(1).Take(16).ToArray()));

            return this;
        }

        public Factory OverrideRegistration<T, TK>() where T : class where TK : class, T
        {
            Container.Register<T, TK>();
            return this;
        }

        /// <summary>
        /// Creates a SimpleBus instance with the default configuration.
        ///
        /// cancelonhafailover=false;heartbeat=10;host=localhost;password=guest;port=5672;
        /// publisherconfirms=false;reconnect=5000;timeout=10000;username=guest;virtualhost=/
        /// </summary>
        public ISimpleBus GetBus()
        {
            return Container.GetInstance<ISimpleBus>();
        }

        /// <summary>
        /// Creates a SimpleBus instance with defaults overridden by any provided values.
        /// </summary>
        public ISimpleBus GetBus(string connectionString)
        {
            var configuration = Container.GetInstance<IConnectionConfiguration>();

            Global.ParseConfiguration(configuration, connectionString);

            return Container.GetInstance<ISimpleBus>();
        }

        public void Dispose()
        {
            Container.GetInstance<IConnection>().Dispose();
        }
    }
}