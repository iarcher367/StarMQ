namespace StarMQ.Test.Core
{
    using System;
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Core;
    using IConnection = StarMQ.Core.IConnection;

    public class PersistentChannelTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private IChannel _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _model = new Mock<IModel>(MockBehavior.Strict);
            _sut = new PersistentChannel(_configuration.Object, _connection.Object, _log.Object);
        }

        [Test]
        public void ShouldInvokeAction()
        {
            _configuration.Setup(x => x.Timeout).Returns(1);
            _connection.Setup(x => x.CreateModel()).Returns(It.IsAny<IModel>());

            var flag = false;

            _sut.InvokeChannelAction(x => { flag = true; });

            Assert.That(flag, Is.True);

            _configuration.Verify(x => x.Timeout, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Once());
        }

        [Test]
        public void Should()
        {
            Assert.Fail();  // TODO: test retries on channel-only exceptions
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.InvokeChannelAction(null);
        }

        [Test]
        public void ShouldDispose()
        {
            _configuration.Setup(x => x.Timeout).Returns(1);
            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _model.Setup(x => x.Dispose());

            _sut.InvokeChannelAction(x => { });
            _sut.Dispose();

            _configuration.Verify(x => x.Timeout, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Once());
            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}