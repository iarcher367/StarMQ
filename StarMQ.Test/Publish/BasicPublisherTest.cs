namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Publish;
    using System;
    using System.Threading.Tasks;

    public class BasePublisherTest
    {
        [Test]
        public void Should()
        {
            Assert.Fail();
        }
    }

    public class BasicPublisherTest
    {
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private BasePublisher _sut;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _sut = new BasicPublisher(_log.Object);
        }

        [Test]
        public async Task ShouldInvokeAction()
        {
            var flag = false;

            await _sut.Publish(_model.Object, x => flag = true);

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfModelIsNull()
        {
            _sut.Publish(null, x => { });
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Publish(_model.Object, null);
        }
    }
}