#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Message
{
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IPipeline : IMessagingInterceptor
    {
        void Add(IMessagingInterceptor interceptor);
    }

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