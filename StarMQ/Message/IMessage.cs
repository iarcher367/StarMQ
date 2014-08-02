namespace StarMQ.Message
{
    public interface IMessage<out T>
    {
        T Body { get; }
        Properties Properties { get; set; }
    }
}