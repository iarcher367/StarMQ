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

namespace StarMQ.Core
{
    using Message;
    using Model;
    using System;

    public interface INamingStrategy
    {
        string GetAlternateName(Exchange exchange);
        string GetConsumerTag();
        string GetDeadLetterName(string baseName);
        string GetExchangeName(Type messageType);
        string GetQueueName(Type messageType);
    }

    public class NamingStrategy : INamingStrategy
    {
        private readonly ITypeNameSerializer _typeNameSerializer;

        public NamingStrategy(ITypeNameSerializer typeNameSerializer)
        {
            _typeNameSerializer = typeNameSerializer;
        }

        public string GetAlternateName(Exchange exchange)
        {
            if (exchange == null)
                throw new ArgumentNullException("exchange");

            return String.Format("AE:{0}", exchange.Name ?? String.Empty);
        }

        public string GetConsumerTag()
        {
            return Guid.NewGuid().ToString();
        }

        public string GetDeadLetterName(string baseName)
        {
            if (baseName == null)
                throw new ArgumentNullException("baseName");

            return String.Format("DLX:{0}", baseName);
        }

        public string GetExchangeName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return _typeNameSerializer.Serialize(messageType);
        }

        public string GetQueueName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return _typeNameSerializer.Serialize(messageType);
        }
    }
}
