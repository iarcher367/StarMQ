namespace StarMQ.Test.Core
{
    using NUnit.Framework;
    using StarMQ.Core;

    public class ConnectionConfigurationTest
    {
        private IConnectionConfiguration _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new ConnectionConfiguration();
        }

        [Test]
        public void ShouldSetDefaultHeartbeat()
        {
            Assert.That(_sut.Heartbeat, Is.EqualTo(10));
        }

        [Test]
        public void ShouldSetDefaultHost()
        {
            Assert.That(_sut.Host, Is.EqualTo("localhost"));
        }

        [Test]
        public void ShouldSetDefaultPassword()
        {
            Assert.That(_sut.Password, Is.EqualTo("guest"));
        }

        [Test]
        public void ShouldSetDefaultPort()
        {
            Assert.That(_sut.Port, Is.EqualTo(5672));
        }

        [Test]
        public void ShouldSetDefaultPrefetchCount()
        {
            Assert.That(_sut.PrefetchCount, Is.EqualTo(50));
        }

        [Test]
        public void ShouldSetDefaultRetryInterval()
        {
            Assert.That(_sut.Reconnect, Is.EqualTo(5000));
        }

        [Test]
        public void ShouldSetDefaultTimeout()
        {
            Assert.That(_sut.Timeout, Is.EqualTo(10000));
        }

        [Test]
        public void ShouldSetDefaultUsername()
        {
            Assert.That(_sut.Username, Is.EqualTo("guest"));
        }

        [Test]
        public void ShouldSetDefaultVirtualHost()
        {
            Assert.That(_sut.VirtualHost, Is.EqualTo("/"));
        }

    }
}