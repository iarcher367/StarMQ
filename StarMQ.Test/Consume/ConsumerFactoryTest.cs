namespace StarMQ.Test.Consume
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using IConnection = StarMQ.Core.IConnection;

    public class ConsumerFactoryTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<INamingStrategy> _namingStrategy;
        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _namingStrategy = new Mock<INamingStrategy>();

            _queue = new Queue(String.Empty);
        }

        [Test]
        public void ShouldCreatePersistentConsumer()
        {
            var actual = ConsumerFactory.CreateConsumer(_queue, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(PersistentConsumer)));
        }

        [Test]
        public void ShouldCreateBasicConsumer()
        {
            _queue.Exclusive = true;

            var actual = ConsumerFactory.CreateConsumer(_queue, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(BasicConsumer)));
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