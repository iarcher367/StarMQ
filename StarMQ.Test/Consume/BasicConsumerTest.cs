namespace StarMQ.Test.Consume
{
    using Exception;
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BasicConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const string QueueName = "StarMQ.Master";

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IInboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private IConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _dispatcher = new Mock<IInboundDispatcher>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _log.Object, _namingStrategy.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Verify(x => x.CreateModel(), Times.Once);
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public async Task ShouldSetQosAndConsumeIfModelIsOpen()
        {
            const int prefetchCount = 10;
            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _model.Setup(x => x.IsOpen).Returns(true);
            _model.Setup(x => x.BasicQos(0, prefetchCount, It.IsAny<bool>()));
            _model.Setup(x => x.BasicConsume(QueueName, false, _sut));

            await _sut.Consume(new Queue(QueueName), message => new AckResponse());

            _model.Verify(x => x.IsOpen, Times.Once);
            _model.Verify(x => x.BasicQos(0, prefetchCount, It.IsAny<bool>()), Times.Once);
            _model.Verify(x => x.BasicConsume(QueueName, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), It.IsAny<BasicConsumer>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.Consume(new Queue(String.Empty), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.Consume(null, message => new AckResponse());
        }

        [Test]
        public void ShouldDoNothingOnConsumerCancelledEvent()
        {
            var sut = new Mock<IConsumer>();

            sut.Raise(x => x.ConsumerCancelled += null, new ConsumerEventArgs(String.Empty));

            Assert.Inconclusive();
        }
    }
}