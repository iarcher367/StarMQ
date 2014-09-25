namespace StarMQ.Test.Consume
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;
    using System.Collections.Generic;
    using IConnection = StarMQ.Core.IConnection;

    public class PersistentConsumerTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<IHandlerManager> _handlerManager;
        private Mock<ILog> _log;
        private Mock<IModel> _modelOne;
        private Mock<IModel> _modelTwo;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPipeline> _pipeline;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private IConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _handlerManager = new Mock<IHandlerManager>();
            _log = new Mock<ILog>();
            _modelOne = new Mock<IModel>();
            _modelTwo = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>();
            _pipeline = new Mock<IPipeline>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _connection.SetupSequence(x => x.CreateModel())
                .Returns(_modelOne.Object)
                .Returns(_modelTwo.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(String.Empty);

            _sut = new PersistentConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, _handlerManager.Object, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object);
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
            var queue = new Queue().WithName(String.Empty);

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            _connection.Raise(x => x.OnConnected += null);

            action();

            Assert.That(_sut.Model, Is.SameAs(_modelTwo.Object));

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Exactly(2));
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Exactly(2));
            _modelTwo.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Once);
            _modelTwo.Verify(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            _sut.Consume(null);
        }

        [Test]
        public void ShouldConsumeOnBasicCancel()
        {
            const ushort prefetchCount = 10;
            Action action = () => { };
            var queue = new Queue().WithName(String.Empty);

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            _sut.HandleBasicCancel(String.Empty);

            action();

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Exactly(2));
            _modelOne.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Once);
            _modelOne.Verify(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Once);
        }
    }
}