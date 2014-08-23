namespace StarMQ.Message
{
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class InterceptorPipeline : IPipeline
    {
        private readonly IList<IMessagingInterceptor> _pipeline = new List<IMessagingInterceptor>();

        public void Add(IMessagingInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException("interceptor");

            _pipeline.Add(interceptor);
        }

        public IMessage<byte[]> OnSend(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            return _pipeline.Aggregate(seed, (acc, f) => f.OnSend(acc));
        }

        public IMessage<byte[]> OnReceive(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            return _pipeline.Reverse()
                .Aggregate(seed, (acc, f) => f.OnReceive(acc));
        }
    }
}