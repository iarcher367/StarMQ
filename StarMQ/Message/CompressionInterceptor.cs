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
