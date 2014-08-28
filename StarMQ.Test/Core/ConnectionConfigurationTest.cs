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
        public void ShouldSetDefaultPersistMessages()
        {
            Assert.That(_sut.PersistMessages, Is.True);
        }

        [Test]
        public void ShouldSetDefaultPort()
        {
            Assert.That(_sut.Port, Is.EqualTo(5672));
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