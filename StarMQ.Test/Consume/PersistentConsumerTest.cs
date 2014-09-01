namespace StarMQ.Test.Consume
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using IConnection = StarMQ.Core.IConnection;

    public class PersistentConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";

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

            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);
        }

        [TearDown]
        public void TearDown()
        {
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public void ShouldConsumeAndSyncModelOnConnectedEvent()
        {
            const ushort prefetchCount = 10;
            var queue = new Queue(String.Empty);

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _connection.SetupSequence(x => x.CreateModel())
                .Returns(new Mock<IModel>().Object)
                .Returns(_model.Object);
            _model.Setup(x => x.IsOpen).Returns(true);
            _model.Setup(x => x.BasicQos(0, prefetchCount, false));
            _model.Setup(x => x.BasicConsume(queue.Name, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()));

            _sut = new PersistentConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, _log.Object, _namingStrategy.Object);
            _sut.Consume(queue, x => new AckResponse());

            _connection.Raise(x => x.OnConnected += null);

            Assert.That(_sut.Model, Is.SameAs(_model.Object));

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Exactly(2));
            _model.Verify(x => x.IsOpen, Times.Exactly(2));
            _model.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Exactly(2));
            _model.Verify(x => x.BasicConsume(queue.Name, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Exactly(2));
        }
    }
}