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

namespace StarMQ.Message
{
    using Model;
    using System;

    public interface ISerializationStrategy
    {
        IMessage<dynamic> Deserialize(IMessage<byte[]> message, Type defaultType);
        IMessage<byte[]> Serialize<T>(IMessage<T> message) where T : class;
    }

    internal class SerializationStrategy : ISerializationStrategy
    {
        private readonly ICorrelationStrategy _correlationStrategy;
        private readonly ISerializer _serializer;
        private readonly ITypeNameSerializer _typeNameSerializer;

        public SerializationStrategy(ICorrelationStrategy correlationStrategy, ISerializer serializer,
            ITypeNameSerializer typeNameSerializer)
        {
            _correlationStrategy = correlationStrategy;
            _serializer = serializer;
            _typeNameSerializer = typeNameSerializer;
        }

        public IMessage<dynamic> Deserialize(IMessage<byte[]> message, Type defaultType)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (defaultType == null)
                throw new ArgumentNullException("defaultType");

            var type = String.IsNullOrEmpty(message.Properties.Type)
                ? defaultType
                : _typeNameSerializer.Deserialize(message.Properties.Type);

            var body = _serializer.ToObject(message.Body, type);

            return new Message<dynamic>(body) { Properties = message.Properties };
        }

        public IMessage<byte[]> Serialize<T>(IMessage<T> message) where T : class
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var body = _serializer.ToBytes(message.Body);

            var properties = message.Properties;
            properties.Type = _typeNameSerializer.Serialize(message.Body.GetType());

            if (String.IsNullOrEmpty(properties.CorrelationId))
                properties.CorrelationId = _correlationStrategy.GenerateCorrelationId();

            return new Message<byte[]>(body) { Properties = properties };
        }
    }
}