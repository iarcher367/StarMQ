namespace StarMQ.Core
{
    using Message;
    using System;

    public interface INamingStrategy
    {
        string GetExchangeName(Type messageType);
        string GetQueueName(Type messageType, string subscriberId);
    }

    public class NamingStrategy : INamingStrategy
    {
        private readonly ITypeNameSerializer _typeNameSerializer;

        public NamingStrategy(ITypeNameSerializer typeNameSerializer)
        {
            _typeNameSerializer = typeNameSerializer;
        }

        public string GetExchangeName(Type messageType)
        {
            return _typeNameSerializer.Serialize(messageType);
        }

        public string GetQueueName(Type messageType, string subscriberId)
        {
            return String.Format("{0}:{1}", _typeNameSerializer.Serialize(messageType), subscriberId);
        }
    }
}
