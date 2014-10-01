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
    using System.Security.Cryptography;

    internal class AesInterceptor : IMessagingInterceptor
    {
        private const string Key = "iv";

        private readonly byte[] _iv;
        private readonly byte[] _key;

        public AesInterceptor(byte[] key, byte[] iv = null)
        {
            _key = key;
            _iv = iv;
        }

        public IMessage<byte[]> OnSend(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            using (var aes = new AesManaged())
            {
                var iv = _iv ?? aes.IV;
                var encryptor = aes.CreateEncryptor(_key, iv);

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        cryptoStream.Write(seed.Body, 0, seed.Body.Length);

                    seed.Properties.Headers.Add(Key, iv);

                    return new Message<byte[]>(memoryStream.ToArray())
                        {
                            Properties = seed.Properties
                        };
                }
            }
        }

        public IMessage<byte[]> OnReceive(IMessage<byte[]> seed)
        {
            if (seed == null)
                throw new ArgumentNullException("seed");

            using (var aes = new AesManaged())
            {
                var iv = (byte[])seed.Properties.Headers[Key];
                var decryptor = aes.CreateDecryptor(_key, iv);

                using (var memoryStream = new MemoryStream(seed.Body))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        using (var outStream = new MemoryStream())
                        {
                            cryptoStream.CopyTo(outStream);

                            return new Message<byte[]>(outStream.ToArray())
                                {
                                    Properties = seed.Properties
                                };
                        }
            }
        }
    }
}