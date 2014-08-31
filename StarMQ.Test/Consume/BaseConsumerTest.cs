namespace StarMQ.Test.Consume
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BaseConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IInboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private BaseConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _dispatcher = new Mock<IInboundDispatcher>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _model = new Mock<IModel>(MockBehavior.Strict);
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);

            _sut = new TransientConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _log.Object, _namingStrategy.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Verify(x => x.CreateModel(), Times.Once);
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public void CancelShouldFireConsumerCancelledEvent()
        {
            Assert.Fail();
        }

        [Test]
        public void CancelOkShould()
        {
            Assert.Fail();
        }

        [Test]
        public void ConsumeOkShouldSetConsumerTag()
        {
            _sut.HandleBasicConsumeOk(ConsumerTag);

            Assert.That(_sut.ConsumerTag, Is.EqualTo(ConsumerTag));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConsumeOkShouldThrowExceptionIfConsumerTagIsNull()
        {
            _sut.HandleBasicConsumeOk(null);
        }

        [Test]
        public void DeliverShouldQueueMessageToDispatcher()
        {
            var properties = new Mock<IBasicProperties>();
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Returns(Task.FromResult(0));

            _sut.Consume(new Queue(String.Empty), x => new AckResponse());
            _sut.HandleBasicDeliver(ConsumerTag, 0, false, String.Empty, String.Empty,
                properties.Object, new byte[0]);

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
        }

        [Test]
        public void ModelShutdownShouldDispose()
        {
            _dispatcher.Setup(x => x.Dispose());
            _model.Setup(x => x.Dispose());

            _sut.HandleModelShutdown(It.IsAny<IModel>(),
                new ShutdownEventArgs(ShutdownInitiator.Application, 0, String.Empty));

            _dispatcher.Verify(x => x.Dispose(), Times.Once);
            _model.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ShouldDispose()
        {
            _dispatcher.Setup(x => x.Dispose());
            _model.Setup(x => x.Dispose());

            _sut.Dispose();

            _dispatcher.Verify(x => x.Dispose(), Times.Once);
            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}