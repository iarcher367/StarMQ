namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Publish;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BasicPublisherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private BasePublisher _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();

            _connection.Setup(x => x.CreateModel()).Returns(new Mock<IModel>().Object);

            _sut = new BasicPublisher(_connection.Object, _log.Object);
        }

        [Test]
        public async Task ShouldInvokeAction()
        {
            var flag = false;

            await _sut.Publish(x => flag = true);

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Publish(null);
        }
    }
}