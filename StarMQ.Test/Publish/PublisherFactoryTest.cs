namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Publish;

    public class PublisherFactoryTest
    {
        private ConnectionConfiguration _configuration;
        private Mock<ILog> _log;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _configuration = new ConnectionConfiguration();
        }

        [Test]
        public void ShouldReturnBasicPublisherIfConfirmsDisabled()
        {
            var actual = PublisherFactory.CreatePublisher(_configuration, _log.Object);

            Assert.That(actual, Is.TypeOf(typeof(BasicPublisher)));
        }

        [Test]
        public void ShouldReturnConfirmsPublisherIfConfirmsEnabled()
        {
            _configuration.PublisherConfirms = true;
            var actual = PublisherFactory.CreatePublisher(_configuration, _log.Object);

            Assert.That(actual, Is.TypeOf(typeof(ConfirmPublisher)));
        }
    }
}