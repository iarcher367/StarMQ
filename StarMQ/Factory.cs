﻿#region Apache License v2.0
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
    using Exception;
    using Message;
    using Publish;
    using RabbitMQ.Client;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using IConnection = Core.IConnection;

    public class Factory : IDisposable
    {
        internal Container Container { get; private set; }

        public Factory()
        {
            Container = new Container();

            Container.Register<IAdvancedBus, AdvancedBus>(true)
                .Register<IConnection, PersistentConnection>(true)
                .Register<IConnectionConfiguration, ConnectionConfiguration>(true)
                .Register<IOutboundDispatcher, OutboundDispatcher>(true)
                .Register<IPipeline, InterceptorPipeline>(true)
                .Register<ISimpleBus, SimpleBus>(true);

            Container.Register<IConsumer, BasicConsumer>()
                .Register<ICorrelationStrategy, CorrelationStrategy>()
                .Register<IHandlerManager, HandlerManager>()
                .Register<INamingStrategy, NamingStrategy>()
                .Register<IPublisher, BasicPublisher>()
                .Register<ISerializer, JsonSerializer>()
                .Register<ISerializationStrategy, SerializationStrategy>()
                .Register<ITypeNameSerializer, TypeNameSerializer>();

            Container.RegisterDecorator<IPublisher, ConfirmPublisherDecorator>(
                () => Container.Resolve<IConnectionConfiguration>().PublisherConfirms);

            Container.Register<IConsumerFactory>(x =>
                new ConsumerFactory(Container.Resolve<IConsumer>, Container.Resolve<IConnection>()))
                .Register<ILog>(x => new EmptyLog())
                .Register(x =>
                {
                    var apiAssembly = Assembly.GetExecutingAssembly().GetName();
                    var config = Container.Resolve<IConnectionConfiguration>();
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
        }

        public Factory AddInterceptor(IMessagingInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException("interceptor");

            Container.Resolve<IPipeline>().Add(interceptor);
            return this;
        }

        public Factory EnableCompression()
        {
            Container.Resolve<IPipeline>().Add(new CompressionInterceptor());
            return this;
        }

        public Factory EnableEncryption(string secretKey)
        {
            if (secretKey == null)
                throw new ArgumentNullException();
            if (secretKey.Length < 16 || secretKey.Length > 32)
                throw new InvalidValueException("secretKey", secretKey);

            var key = Encoding.UTF8.GetBytes(secretKey);

            Container.Resolve<IPipeline>().Add(new AesInterceptor(key));

            return this;
        }

        public Factory OverrideRegistration<T, TK>() where T : class where TK : class, T
        {
            Container.Register<T, TK>();
            return this;
        }

        public Factory OverrideRegistration<T>(Func<Context, T> func) where T : class
        {
            Container.Register(func);
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
            return Container.Resolve<ISimpleBus>();
        }

        /// <summary>
        /// Creates a SimpleBus instance with defaults overridden by any provided values.
        /// </summary>
        public ISimpleBus GetBus(string connectionString)
        {
            var configuration = Container.Resolve<IConnectionConfiguration>();

            Global.ParseConfiguration(configuration, connectionString);

            return Container.Resolve<ISimpleBus>();
        }

        public void Dispose()
        {
            Container.Resolve<IConnection>().Dispose();
        }
    }
}