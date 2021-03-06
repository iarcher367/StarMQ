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

namespace StarMQ.Consume
{
    using Core;
    using Message;
    using Model;
    using RabbitMQ.Client;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// This consumer is designed for exclusive or self-destructing queues as it does not
    /// re-subscribe to the specified queue after the system recovers the connection to the broker.
    /// </summary>
    internal class BasicConsumer : BaseConsumer
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IOutboundDispatcher _dispatcher;

        public BasicConsumer(IConnectionConfiguration configuration, IConnection connection,
            IOutboundDispatcher dispatcher, IHandlerManager handlerManager, ILog log,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy)
            : base(connection, handlerManager, log, namingStrategy, pipeline, serializationStrategy)
        {
            _configuration = configuration;
            _dispatcher = dispatcher;
        }

        public override async Task Consume(Queue queue, Action<IHandlerRegistrar> configure = null, IBasicConsumer instance = null)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            if (configure != null)
                configure(HandlerManager);

            HandlerManager.Validate();

            await _dispatcher.Invoke(() =>
            {
                var args = new Dictionary<string, object>();

                OpenChannel();

                Model.BasicQos(0, _configuration.PrefetchCount, false);

                if (_configuration.CancelOnHaFailover || queue.CancelOnHaFailover)
                    args.Add("x-cancel-on-ha-failover", true);
                if (queue.Priority != 0)
                    args.Add("x-priority", queue.Priority);

                Model.BasicConsume(queue.Name, false, ConsumerTag, args, instance ?? this);

                Log.Info(String.Format("Consumer '{0}' declared on queue '{1}'.", ConsumerTag, queue.Name));
            });
        }

        private void OpenChannel()
        {
            if (Model != null && Model.IsOpen) return;

            Model = Connection.CreateModel();

            Log.Info("Channel opened.");
        }
    }
}