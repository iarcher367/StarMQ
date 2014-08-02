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
    using Moq;
    using NUnit.Framework;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;

    public class InterceptorPipelineTest
    {
        private Mock<IMessagingInterceptor> _interceptorA;
        private Mock<IMessagingInterceptor> _interceptorB;
        private IPipeline _sut;

        [SetUp]
        public void Setup()
        {
            _interceptorA = new Mock<IMessagingInterceptor>();
            _interceptorB = new Mock<IMessagingInterceptor>();

            _sut = new InterceptorPipeline();
        }

        [Test]
        public void AddShouldAddInterceptor()
        {
            _sut.Add(_interceptorA.Object);

            _sut.OnSend(new Message<byte[]>(new byte[0]));

            _interceptorA.Verify(x => x.OnSend(It.IsAny<IMessage<byte[]>>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddShouldThrowExceptionIfInterceptorIsNull()
        {
            _sut.Add(null);
        }

        [Test]
        public void OnSendShouldCallInterceptorsInOrder()
        {
            var messageA = new Message<byte[]>(new byte[0]);
            var messageB = new Message<byte[]>(new byte[0]);
            var messageC = new Message<byte[]>(new byte[0]);

            _sut.Add(_interceptorA.Object);
            _sut.Add(_interceptorB.Object);

            _interceptorA.Setup(x => x.OnSend(messageA)).Returns(messageB);
            _interceptorB.Setup(x => x.OnSend(messageB)).Returns(messageC);

            var actual = _sut.OnSend(messageA);

            Assert.That(actual, Is.SameAs(messageC));

            _interceptorA.Verify(x => x.OnSend(messageA), Times.Once);
            _interceptorB.Verify(x => x.OnSend(messageB), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OnSendShouldThrowExceptionIfSeedIsNull()
        {
            _sut.OnSend(null);
        }

        [Test]
        public void OnReceiveShouldCallInterceptorsInReverseOrder()
        {
            var messageA = new Message<byte[]>(new byte[0]);
            var messageB = new Message<byte[]>(new byte[0]);
            var messageC = new Message<byte[]>(new byte[0]);

            _sut.Add(_interceptorA.Object);
            _sut.Add(_interceptorB.Object);

            _interceptorB.Setup(x => x.OnReceive(messageA)).Returns(messageB);
            _interceptorA.Setup(x => x.OnReceive(messageB)).Returns(messageC);

            var actual = _sut.OnReceive(messageA);

            Assert.That(actual, Is.SameAs(messageC));

            _interceptorB.Verify(x => x.OnReceive(messageA), Times.Once);
            _interceptorA.Verify(x => x.OnReceive(messageB), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OnReceiveShouldThrowExceptionIfSeedIsNull()
        {
            _sut.OnReceive(null);
        }
    }
}