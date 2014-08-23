namespace StarMQ.Message
{
    public interface IPipeline : IMessagingInterceptor
    {
        void Add(IMessagingInterceptor interceptor);
    }
}