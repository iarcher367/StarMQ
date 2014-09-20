namespace StarMQ.Core
{
    using Message;
    using System;

    public interface INamingStrategy
    {
        string GetAlternateExchangeName(Type messageType);
        string GetAlternateQueueName(Type messageType);
        string GetConsumerTag();
        string GetDeadLetterExchangeName(Type messageType);
        string GetDeadLetterQueueName(Type messageType);
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

        public string GetAlternateExchangeName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return String.Format("AE:{0}", _typeNameSerializer.Serialize(messageType));
        }

        public string GetAlternateQueueName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return String.Format("AE:{0}", _typeNameSerializer.Serialize(messageType));
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

        public string GetDeadLetterQueueName(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            return String.Format("DLX:{0}", _typeNameSerializer.Serialize(messageType));
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
