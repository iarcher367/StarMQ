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