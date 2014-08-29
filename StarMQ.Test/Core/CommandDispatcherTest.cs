namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using System;

    public class CommandDispatcherTest
    {
        private Mock<IChannel> _channel;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private ICommandDispatcher _sut;

        [SetUp]
        public void Setup()
        {
            _channel = new Mock<IChannel>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _sut = new CommandDispatcher(_channel.Object, _connection.Object, _log.Object);
        }

        [Test]
        public void Should()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldDoSomethingWhenOnConnectEventFires()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldQueueAction()
        {
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