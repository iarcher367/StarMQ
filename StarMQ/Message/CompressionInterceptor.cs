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
    using System.IO;
    using System.IO.Compression;

    public class CompressionInterceptor : IMessagingInterceptor
    {
        public IMessage<byte[]> OnSend(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            using (var output = new MemoryStream())
            {
                using (var compress = new DeflateStream(output, CompressionMode.Compress))
                    compress.Write(seed.Body, 0, seed.Body.Length);
                return new Message<byte[]>(output.ToArray()) { Properties = seed.Properties };
            }
        }

        public IMessage<byte[]> OnReceive(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            using (var input = new MemoryStream(seed.Body))
            {
                using (var output = new MemoryStream())
                {
                    using (var decompress = new DeflateStream(input, CompressionMode.Decompress))
                        decompress.CopyTo(output);
                    return new Message<byte[]>(output.ToArray()) { Properties = seed.Properties };
                }
            }
        }
    }
}
