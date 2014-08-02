namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class StarMqException : Exception
    {
        public StarMqException(string message) : base(message) { }
        public StarMqException(string format, params string[] args) : base(String.Format(format, args)) { }
    }
}