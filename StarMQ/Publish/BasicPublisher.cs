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
    using RabbitMQ.Client;
    using System;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// Offers no advanced functionality or messaging guarantees.
    /// </summary>
    public sealed class BasicPublisher : BasePublisher
    {
        public BasicPublisher(IConnection connection, ILog log) : base(connection, log)
        {
            OnConnected();
        }

        public override Task Publish(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            action(Model);

            return Task.FromResult<object>(null);
        }
    }
}