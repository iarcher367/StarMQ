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
    using System.Collections.Generic;
    using IConnection = StarMQ.Core.IConnection;

    public class TransientConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const string QueueName = "StarMQ.Master";

        private Mock<IConnection> _connection;
        private Mock<IConsumerDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private IConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _dispatcher = new Mock<IConsumerDispatcher>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);

            _sut = new TransientConsumer(_connection.Object, _dispatcher.Object, _log.Object,
                _namingStrategy.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Verify(x => x.CreateModel(), Times.Once());
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public void ShouldSetQosAndConsume()
        {
            _model.Setup(x => x.IsOpen).Returns(true);
            _model.Setup(x => x.BasicQos(0, 50, It.IsAny<bool>())); // TODO: update when not hardcode
            _model.Setup(x => x.BasicConsume(QueueName, false, _sut));

            _sut.Consume(new Queue(QueueName), message => new AckResponse());

            _model.Verify(x => x.IsOpen, Times.Once);
            _model.Verify(x => x.BasicQos(0, 50, It.IsAny<bool>()), Times.Once);
            _model.Verify(x => x.BasicConsume(QueueName, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), It.IsAny<TransientConsumer>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfMessageHandlerIsNull()
        {
            _sut.Consume(new Queue(String.Empty), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            _sut.Consume(null, message => new AckResponse());
        }
    }
}