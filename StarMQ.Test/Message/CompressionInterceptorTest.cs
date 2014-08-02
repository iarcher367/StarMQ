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

    public class CompressionInterceptorTest
    {
        private const double CompressionRatio = 0.7;
        private const string Content = "Lorem ipsum dolor sit amet, est ea dicit verear albucius. " +
            "Populo phaedrum efficiantur has no, usu ex decore accusamus, ad nec etiam mazim " +
            "constituto. Liber prompta reprehendunt ea sea, no case fierent qui, elit viderer an " +
            "vix. Eum ea harum veritus. Dicunt labitur quaestio eu nam. Vide graece democritum.";
        private readonly byte[] _compressedContent = { 29, 207, 49, 110, 195, 48, 12, 5, 208, 171, 124, 100, 
            54, 124, 138, 118, 203, 208, 169, 59, 45, 49, 53, 1, 75, 84, 72, 177, 8, 122, 250, 80, 221, 132,
            15, 241, 243, 241, 118, 87, 227, 6, 25, 30, 13, 85, 47, 53, 184, 76, 80, 227, 185, 129, 125, 130,
            9, 85, 74, 70, 191, 108, 76, 6, 186, 142, 40, 18, 190, 227, 75, 71, 92, 138, 113, 18, 87, 203, 105,
            126, 60, 242, 35, 245, 25, 134, 147, 28, 93, 55, 132, 7, 248, 133, 202, 37, 215, 128, 74, 9, 167,
            22, 190, 129, 42, 58, 23, 240, 20, 106, 104, 244, 39, 13, 69, 187, 79, 153, 49, 117, 199, 93, 14,
            54, 12, 211, 54, 38, 193, 120, 24, 159, 220, 107, 244, 127, 144, 51, 109, 89, 143, 66, 206, 120,
            72, 194, 50, 127, 134, 164, 248, 90, 82, 169, 25, 37, 181, 231, 243, 181, 227, 115, 225, 40, 77,
            75, 153, 103, 228, 142, 228, 127, 72, 89, 117, 23, 29, 178, 196, 207, 160, 60, 87, 20, 28, 232,
            212, 118, 124, 103, 11, 126, 140, 184, 112, 250, 155, 150, 53, 215, 246, 219, 27 };

        private IMessagingInterceptor _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new CompressionInterceptor();
        }

        [Test]
        public void OnSendShouldCompressBody()
        {
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var compressedData = _sut.OnSend(message).Body;

            Assert.That(compressedData.Length, Is.LessThan(message.Body.Length * CompressionRatio));
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
            var message = new Message<byte[]>(_compressedContent);

            var uncompressedData = _sut.OnReceive(message).Body;
            var actual = new JsonSerializer().ToObject(uncompressedData, typeof(string));

            Assert.That(actual, Is.EqualTo(Content));
        }

        [Test]
        public void OnReceiveShouldPreserveProperties()
        {
            var correlationId = Guid.NewGuid().ToString();
            var message = new Message<byte[]>(_compressedContent)
            {
                Properties = { CorrelationId = correlationId }
            };

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