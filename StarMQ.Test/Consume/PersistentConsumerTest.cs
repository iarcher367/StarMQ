
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

    public class PersistentConsumerTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private IConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);

            _sut = new PersistentConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, _log.Object, _namingStrategy.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldSyncModelAndConsumeOnConnectedEvent()
        {
            const ushort prefetchCount = 10;
            Action action = () => { };
            var queue = new Queue(String.Empty);

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue, x => new AckResponse());

            _connection.Raise(x => x.OnConnected += null);

            action();

            Assert.That(_sut.Model, Is.SameAs(_model.Object));

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Exactly(2));
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Exactly(2));
            _model.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Once);
            _model.Verify(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            _sut.Consume(null, x => new AckResponse());
        }
    }
}