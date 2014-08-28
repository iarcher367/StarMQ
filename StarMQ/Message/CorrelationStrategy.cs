namespace StarMQ.Message
{
    using System;

    public interface ICorrelationStrategy
    {
        string GenerateCorrelationId();
    }

    public class CorrelationStrategy : ICorrelationStrategy
    {
        public string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}