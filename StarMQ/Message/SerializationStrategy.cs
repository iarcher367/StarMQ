namespace StarMQ.Message
{
    using Model;
    using System;

    public interface ISerializationStrategy
    {
        IMessage<dynamic> Deserialize(IMessage<byte[]> message);
        IMessage<byte[]> Serialize<T>(IMessage<T> message) where T : class;
    }

    public class SerializationStrategy : ISerializationStrategy
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

        public IMessage<dynamic> Deserialize(IMessage<byte[]> message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var type = _typeNameSerializer.Deserialize(message.Properties.Type);
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