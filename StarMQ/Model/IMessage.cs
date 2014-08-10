namespace StarMQ.Model
{
    public interface IMessage<out T>
    {
        T Body { get; }
        Properties Properties { get; set; }
    }
}