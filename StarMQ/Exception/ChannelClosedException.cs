namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class ChannelClosedException : Exception
    {
        private const string Template = "Cannot perform operation '{0}' - channel closed.";

        public ChannelClosedException(string operation)
            : base(String.Format(Template, operation))
        {
        }
    }
}