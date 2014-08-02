namespace StarMQ.Message
{
    public interface ISerializationStrategy
    {
        IMessage<byte[]> Serialize<T>(IMessage<T> message) where T : class;
        IMessage<T> Deserialize<T>(IMessage<byte[]> message) where T : class;
    }
}
