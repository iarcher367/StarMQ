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
        public void ShouldSetDefaultUsername()
        {
            Assert.That(_sut.Username, Is.EqualTo("guest"));
        }
    }
}