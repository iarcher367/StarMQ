#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ
{
    using Consume;
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

            Container.Register<IConsumerFactory>(() =>
            {
                var connection = Container.GetInstance<IConnection>();
                return new ConsumerFactory(() => Container.GetInstance<IConsumer>(), connection);
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

            Container.Register(() =>
            {
                var config = Container.GetInstance<IConnectionConfiguration>();
                var connection = Container.GetInstance<IConnection>();
                var dispatcher = Container.GetInstance<IOutboundDispatcher>();
                var pipeline = Container.GetInstance<IPipeline>();
                var serializationStrategy = Container.GetInstance<ISerializationStrategy>();

                return config.PublisherConfirms
                    ? new ConfirmPublisher(config, connection, dispatcher,
                        LogManager.GetLogger(typeof(ConfirmPublisher)), pipeline, serializationStrategy)
                    : (IPublisher)new BasicPublisher(connection, dispatcher,
                        LogManager.GetLogger(typeof(BasicPublisher)), pipeline, serializationStrategy);
            });

            Container.Register<IConsumer, BasicConsumer>();
            Container.Register<ICorrelationStrategy, CorrelationStrategy>();
            Container.Register<IHandlerManager, HandlerManager>();
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