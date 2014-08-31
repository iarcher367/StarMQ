namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using System;
    using System.Threading.Tasks;

    public class InboundDispatcherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private IInboundDispatcher _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _sut = new InboundDispatcher(_connection.Object, _log.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public async Task ShouldDispatchAction()
        {
            var flag = false;

            await _sut.Invoke(() => { flag = true; });

            await Task.Delay(10);

            Assert.That(flag, Is.True);
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

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ShouldThrowExceptionIfInvokeIsCalledAfterDispose()
        {
            _sut.Dispose();

            _sut.Invoke(() => { });
        }
    }
}