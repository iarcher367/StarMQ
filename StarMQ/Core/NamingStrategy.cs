namespace StarMQ.Core
{
    using Message;
    using System;

    public interface INamingStrategy
    {
        string GetConsumerTag();
        string GetDeadLetterExchangeName(Type messageType);
        string GetDeadLetterQueueName(Type messageType, string subscriberId);
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

        public string GetConsumerTag()
        {
            return Guid.NewGuid().ToString();
        }

        public string GetDeadLetterExchangeName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return String.Format("DLX:{0}", _typeNameSerializer.Serialize(messageType));
        }

        public string GetDeadLetterQueueName(Type messageType, string subscriberId)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");
            if (subscriberId == null)
                throw new ArgumentNullException("subscriberId");

            return String.Format("DLX:{0}:{1}", _typeNameSerializer.Serialize(messageType), subscriberId);
        }

        public string GetExchangeName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return _typeNameSerializer.Serialize(messageType);
        }

        public string GetQueueName(Type messageType, string subscriberId)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");
            if (subscriberId == null)
                throw new ArgumentNullException("subscriberId");

            return String.Format("{0}:{1}", _typeNameSerializer.Serialize(messageType), subscriberId);
        }
    }
}
