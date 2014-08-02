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
    using System;

    public interface IConsumerFactory
    {
        IConsumer CreateConsumer(bool exclusive);
    }

    public class ConsumerFactory : IConsumerFactory
    {
        private readonly Func<IConsumer> _getConsumer;
        private readonly IConnection _connection;

        public ConsumerFactory(Func<IConsumer> getConsumer, IConnection connection)
        {
            _getConsumer = getConsumer;
            _connection = connection;
        }

        public IConsumer CreateConsumer(bool exclusive)
        {
            return exclusive
                ? _getConsumer()
                : new PersistentConsumerDecorator(_getConsumer(), _connection); // TODO: inject via DI
        }
    }
}
