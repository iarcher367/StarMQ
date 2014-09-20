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
