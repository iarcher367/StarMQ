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

namespace StarMQ.Test.Message
{
    using NUnit.Framework;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;
    using System.Linq;

    public class AesInterceptorTest
    {
        private const string Content = "Lorem ipsum dolor sit amet, est ea dicit verear albucius. " +
            "Populo phaedrum efficiantur has no, usu ex decore accusamus, ad nec etiam mazim constituto.";
        private const string Key = "iv";

        private readonly byte[] _iv = { 83, 117, 110, 46, 32, 77, 111, 111, 110, 46, 32, 83, 116, 97, 114, 115 };
        private readonly byte[] _key = { 34, 83, 116, 97, 114, 77, 81, 46, 77, 97, 115, 116, 101, 114, 81, 34 };
        private readonly byte[] _encryptedContent = { 201, 123, 166, 166, 92, 184, 74, 74, 61, 170, 209, 62, 70,
            130, 77, 79, 123, 243, 115, 225, 61, 132, 27, 207, 134, 153, 123, 96, 65, 73, 242, 226, 47, 37, 249,
            120, 82, 191, 182, 26, 87, 48, 192, 141, 129, 68, 163, 76, 223, 160, 12, 95, 97, 0, 0, 102, 224,
            169, 61, 45, 49, 194, 132, 71, 251, 54, 231, 189, 163, 246, 4, 174, 124, 32, 98, 66, 193, 24, 24,
            84, 64, 103, 120, 162, 210, 115, 212, 13, 78, 5, 176, 253, 115, 254, 128, 71, 33, 54, 191, 165,
            128, 210, 39, 225, 64, 120, 42, 44, 63, 185, 238, 86, 87, 132, 197, 127, 122, 98, 65, 197, 62, 254,
            36, 152, 53, 158, 136, 31, 24, 184, 23, 63, 57, 54, 56, 127, 91, 247, 181, 51, 84, 223, 251, 82,
            24, 216, 244, 101, 71, 225, 176, 107, 98, 218, 137, 173, 45, 250, 74, 72 };

        private IMessagingInterceptor _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new AesInterceptor(_key, _iv);
        }

        [Test]
        public void OnSendShouldCompressBody()
        {
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var encryptedData = _sut.OnSend(message).Body;

            Assert.That(encryptedData.SequenceEqual(_encryptedContent), Is.True);
        }

        [Test]
        public void OnSendShouldPassIvInHeader()
        {
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var headers = _sut.OnSend(message).Properties.Headers;

            Assert.That(headers.ContainsKey(Key), Is.True);
            Assert.That(headers[Key], Is.EqualTo(_iv));
        }

        [Test]
        public void OnSendShouldPreserveProperties()
        {
            var correlationId = Guid.NewGuid().ToString();
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data)
                          {
                              Properties = { CorrelationId = correlationId }
                          };

            var properties = _sut.OnSend(message).Properties;

            Assert.That(properties.CorrelationId, Is.EqualTo(correlationId));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OnSendShouldThrowExceptionIfSeedIsNull()
        {
            _sut.OnSend(null);
        }

        [Test]
        public void OnReceiveShouldDecompressBody()
        {
            var message = new Message<byte[]>(_encryptedContent);
            message.Properties.Headers.Add("iv", _iv);

            var decryptedData = _sut.OnReceive(message).Body;
            var actual = new JsonSerializer().ToObject(decryptedData, typeof(string));

            Assert.That(actual, Is.EqualTo(Content));
        }

        [Test]
        public void OnReceiveShouldPreserveProperties()
        {
            var correlationId = Guid.NewGuid().ToString();
            var message = new Message<byte[]>(_encryptedContent)
                          {
                              Properties = { CorrelationId = correlationId }
                          };
            message.Properties.Headers.Add("iv", _iv);

            var properties = _sut.OnReceive(message).Properties;

            Assert.That(properties.CorrelationId, Is.EqualTo(correlationId));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OnReceiveShouldThrowExceptionIfSeedIsNull()
        {
            _sut.OnReceive(null);
        }
    }
}