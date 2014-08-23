namespace StarMQ.Message
{
    using Model;

    public interface IMessagingInterceptor
    {
        IMessage<byte[]> OnSend(IMessage<byte[]> seed);
        IMessage<byte[]> OnReceive(IMessage<byte[]> seed);
    }
}