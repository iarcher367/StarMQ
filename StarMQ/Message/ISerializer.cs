namespace StarMQ.Message
{
    public interface ISerializer
    {
        byte[] ToBytes<T>(T content) where T : class;
        T ToObject<T>(byte[] content) where T : class;
    }
}