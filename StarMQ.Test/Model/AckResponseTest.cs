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

namespace StarMQ.Test.Model
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Model;
    using System;

    public class AckResponseTest
    {
        private Mock<IModel> _channel;
        private Mock<ILog> _log;
        private AckResponse _sut;

        [SetUp]
        public void Setup()
        {
            _channel = new Mock<IModel>();
            _log = new Mock<ILog>();

            _sut = new AckResponse
                {
                    DeliveryTag = 42,
                    Multiple = true
                };
        }

        [Test]
        public void ShouldSendAnAck()
        {
            _sut.Send(_channel.Object, _log.Object);

            _channel.Verify(x => x.BasicAck(_sut.DeliveryTag, _sut.Multiple), Times.Once);
        }

        [Test]
        public void ShouldDefaultActionToDoNothing()
        {
            var actual = new AckResponse();

            Assert.That(actual.Action, Is.EqualTo(ResponseAction.DoNothing));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfChannelIsNull()
        {
            _sut.Send(null, _log.Object);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfLogIsNull()
        {
            _sut.Send(_channel.Object, null);
        }
    }
}