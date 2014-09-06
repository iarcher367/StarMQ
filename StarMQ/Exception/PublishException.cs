namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class PublishException : StarMqException
    {
        public PublishException() : base("Unable to process publish. Internal broker error occurred.")
        {
        }
    }
}