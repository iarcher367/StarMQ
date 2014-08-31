namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Core;
    using System;
    using IConnection = StarMQ.Core.IConnection;

    public class OutboundDispatcherTest
    {
        private Mock<IChannel> _channel;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private IOutboundDispatcher _sut;

        [SetUp]
        public void Setup()
        {
            _channel = new Mock<IChannel>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _sut = new OutboundDispatcher(_channel.Object, _connection.Object, _log.Object);

            _channel.Setup(x => x.Dispose());
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void Should()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldBlockWhenOnConnectEventFires()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldUnblockWhenOnDisconnectEventFires()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldQueueAction()
        {
            _channel.Setup(x => x.InvokeChannelAction(It.IsAny<Action<IModel>>()));

            _sut.Invoke(x => { });

            Assert.Inconclusive();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Invoke(null);
        }

        [Test]
        public void ShouldDispose()
        {
            _channel.Setup(x => x.Dispose());

            _sut.Dispose();

            _channel.Verify(x => x.Dispose(), Times.Once);
        }
    }
}