namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;

    public class PersistentConnectionTest
    {
        private Mock<ILog> _log;
        private PersistentConnection _sut;

        [SetUp]
        public void Setup()
        {
            var configuration = new ConnectionConfiguration();

            _log = new Mock<ILog>();
            _sut = new PersistentConnection(configuration, _log.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldOpenAChannel()
        {
            var actual = _sut.CreateModel();

            Assert.That(actual.IsOpen, Is.True);
        }

        [Test]
        public void ShouldFireOnDisconnected()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void ShouldReconnectWhenDisconnected()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void ShouldFireOnConnected()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void ShouldDisconnectOnDispose()
        {
            var channel = _sut.CreateModel();

            Assert.That(channel.IsOpen, Is.True);
            Assert.That(_sut.IsConnected, Is.True);

            _sut.Dispose();

            Assert.That(channel.IsOpen, Is.False);
            Assert.That(_sut.IsConnected, Is.False);
        }
    }
}