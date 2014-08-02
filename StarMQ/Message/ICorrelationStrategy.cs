namespace StarMQ.Message
{
    public interface ICorrelationStrategy
    {
        string GenerateCorrelationId();
    }
}