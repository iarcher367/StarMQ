namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class MaxLengthException : StarMqException
    {
        private const string Template = "Exceeded max length of 255 chars for field '{0}'. Value: '{1}'";

        public MaxLengthException(string field, string value)
            : base(String.Format(Template, field, value))
        {
        }
    }
}
