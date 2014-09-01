namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Publish;

    public class BasePublisherTest
    {
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private BasePublisher _sut;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _model = new Mock<IModel>(MockBehavior.Strict);
            _sut = new BasicPublisher(_log.Object);
        }

        [Test]
        public void ShouldSynchronizeModelChanges()
        {
            var newModel = new Mock<IModel>(MockBehavior.Strict);

            _sut.Publish(_model.Object, x => { });

            Assert.Fail();
        }

        [Test]
        public void ShouldFireBasicReturnEvent()
        {
            Assert.Fail();
        }
    }
}