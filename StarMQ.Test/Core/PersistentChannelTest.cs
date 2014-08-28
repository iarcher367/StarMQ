namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;

    public class PersistentChannelTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private IChannel _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _sut = new PersistentChannel(_configuration.Object, _connection.Object, _log.Object);
        }

        [Test]
        public void Should()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldDispose()
        {
            _sut.Dispose();

            Assert.Inconclusive();
        }
    }
}