namespace StarMQ.Test.Consume
{
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using IConnection = StarMQ.Core.IConnection;

    public class ConsumerFactoryTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IInboundDispatcher> _dispatcher;
        private Mock<INamingStrategy> _namingStrategy;
        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _dispatcher = new Mock<IInboundDispatcher>(MockBehavior.Strict);
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);

            var model = new Mock<IModel>();
            _connection.Setup(x => x.CreateModel()).Returns(model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(String.Empty);

            _queue = new Queue(String.Empty);
        }

        [Test]
        public void ShouldCreatePersistentConsumer()
        {
            var actual = ConsumerFactory.CreateConsumer(_queue, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(PersistentConsumer)));

            _connection.Verify(x => x.CreateModel(), Times.Once);
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public void ShouldCreateTransientConsumer()
        {
            _queue.Exclusive = true;

            var actual = ConsumerFactory.CreateConsumer(_queue, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(BasicConsumer)));

            _connection.Verify(x => x.CreateModel(), Times.Once);
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            ConsumerFactory.CreateConsumer(null, _configuration.Object, _connection.Object,
                _dispatcher.Object, _namingStrategy.Object);
        }
    }
}