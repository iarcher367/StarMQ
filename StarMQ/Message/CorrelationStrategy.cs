namespace StarMQ.Message
{
    using System;

    public class CorrelationStrategy : ICorrelationStrategy
    {
        public string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}