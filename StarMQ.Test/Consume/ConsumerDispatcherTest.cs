namespace StarMQ.Test.Consume
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Core;
    using System;

    public class ConsumerDispatcherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private IConsumerDispatcher _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _sut = new ConsumerDispatcher(_connection.Object, _log.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldQueueAction()
        {
            _sut.Invoke(() => { });

            Assert.Inconclusive();
        }

        [Test]
        public void ShouldSendAckResponse()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldSendNackResponseOnException()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldCancelAndCallDisposeForUnsubscribeAction()
        {
            Assert.Fail();
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
            _sut.Dispose();

            Assert.Inconclusive();
        }
    }
}