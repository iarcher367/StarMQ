namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class InvalidValueException : StarMqException
    {
        private const string Template = "Attempted to set invalid value for field '{0}'. Value: '{1}'";

        public InvalidValueException(string field, string value)
            : base(String.Format(Template, field, value))
        {
        }
    }
}