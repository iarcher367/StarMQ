namespace StarMQ.Model
{
    using System;

    public class Message<T> : IMessage<T> where T : class
    {
        private Properties _properties;

        public T Body { get; private set; }

        public Properties Properties
        {
            get { return _properties; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _properties = value;
            }
        }

        public Message(T body)
        {
            if (body == null)
                throw new ArgumentNullException("body");

            Body = body;
            Properties = new Properties();
        }
    }
}