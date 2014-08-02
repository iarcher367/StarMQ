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

namespace StarMQ.Publish
{
    using log4net;
    using Message;
    using Model;
    using RabbitMQ.Client;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// Offers no advanced functionality or messaging guarantees.
    /// </summary>
    public class BasicPublisher : BasePublisher
    {
        private readonly IPipeline _pipeline;
        private readonly ISerializationStrategy _serializationStrategy;

        public BasicPublisher(IConnection connection, ILog log, IPipeline pipeline,
            ISerializationStrategy serializationStrategy)
            : base(connection, log)
        {
            _pipeline = pipeline;
            _serializationStrategy = serializationStrategy;
        }

        public override Task Publish<T>(IMessage<T> message, Action<IModel, IBasicProperties, byte[]> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var serialized = _serializationStrategy.Serialize(message);
            var data = _pipeline.OnSend(serialized);

            var properties = Model.CreateBasicProperties();
            data.Properties.CopyTo(properties);

            action(Model, properties, data.Body);

            return Task.FromResult(0);
        }
    }
}