namespace StarMQ.Test.Consume
{
    using Exception;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;
    using IConnection = StarMQ.Core.IConnection;

    public class ConsumerFactoryTest
    {
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPipeline> _pipeline;
        private Mock<ISerializationStrategy> _serializationStrategy;

        private Action<IHandlerRegistrar> _configure;
        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _namingStrategy = new Mock<INamingStrategy>();
            _pipeline = new Mock<IPipeline>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _configure = x => x.Add<string>(y => { });
            _queue = new Queue();
        }

        [Test]
        public void ShouldCreatePersistentConsumer()
        {
            var actual = ConsumerFactory.CreateConsumer(_queue, _configure, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(PersistentConsumer)));
        }

        [Test]
        public void ShouldCreateBasicConsumer()
        {
            _queue.Exclusive = true;

            var actual = ConsumerFactory.CreateConsumer(_queue, _configure, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);

            Assert.That(actual, Is.TypeOf(typeof(BasicConsumer)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            ConsumerFactory.CreateConsumer(null, x => { }, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfConfigureIsNull()
        {
            ConsumerFactory.CreateConsumer(_queue, null, _configuration.Object, _connection.Object,
                _dispatcher.Object, _namingStrategy.Object, _pipeline.Object, _serializationStrategy.Object);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfNoHandlersRegistered()
        {
            ConsumerFactory.CreateConsumer(_queue, x => { }, _configuration.Object,
                _connection.Object, _dispatcher.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);
        }
    }
}