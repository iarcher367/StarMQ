namespace StarMQ.Test.Model
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Model;
    using System;

    public class NackResponseTest
    {
        private Mock<IModel> _channel;
        private Mock<ILog> _log;
        private NackResponse _sut;

        [SetUp]
        public void Setup()
        {
            _channel = new Mock<IModel>(MockBehavior.Strict);
            _log = new Mock<ILog>();

            _sut = new NackResponse
                {
                    DeliveryTag = 42,
                    Multiple = true,
                    Requeue = true
                };
        }

        [Test]
        public void ShouldSendANackIfChannelIsOpen()
        {
            _channel.Setup(x => x.BasicNack(_sut.DeliveryTag, _sut.Multiple, _sut.Requeue));
            _channel.Setup(x => x.IsOpen).Returns(true);

            _sut.Send(_channel.Object, _log.Object);

            _channel.Verify(x => x.BasicNack(_sut.DeliveryTag, _sut.Multiple, _sut.Requeue), Times.Once);
            _channel.Verify(x => x.IsOpen, Times.Once);
        }

        [Test]
        public void ShouldNotSendANackIfChannelIsClosed()
        {
            _channel.Setup(x => x.IsOpen).Returns(false);

            _sut.Send(_channel.Object, _log.Object);

            _channel.Verify(x => x.BasicNack(_sut.DeliveryTag, _sut.Multiple, _sut.Requeue), Times.Never);
            _channel.Verify(x => x.IsOpen, Times.Once);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
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