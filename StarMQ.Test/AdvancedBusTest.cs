namespace StarMQ.Test
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class AdvancedBusTest
    {
        private const string RoutingKey = "x.y";

        private Mock<ICommandDispatcher> _commandDispatcher;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPipeline> _pipeline;
        private Mock<IPublisher> _publisher;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private IAdvancedBus _sut;

        private Exchange _exchange;
        private IMessage<string> _message;
        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _commandDispatcher = new Mock<ICommandDispatcher>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);
            _pipeline = new Mock<IPipeline>(MockBehavior.Strict);
            _publisher = new Mock<IPublisher>(MockBehavior.Strict);
            _serializationStrategy = new Mock<ISerializationStrategy>(MockBehavior.Strict);

            _sut = new AdvancedBus(_commandDispatcher.Object, _connection.Object, _log.Object,
                _namingStrategy.Object, _pipeline.Object, _publisher.Object,
                _serializationStrategy.Object);

            _exchange = new Exchange("StarMQ.Master");
            _message = new Message<string>("Hello World!");
            _queue = new Queue("StarMQ.Slave");
        }

        [Test]
        public void ShouldConsume()
        {
            _sut.ConsumeAsync<string>(_queue, x => new AckResponse());

            Assert.Inconclusive();
        }

        #region ExchangeDeclareAsync
        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclareExchange()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldSetArgs()
        {
            await Task.FromResult(0);

            Assert.Inconclusive();
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclarePassive()
        {
            _exchange.Passive = true;

            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldOnlyDeclareOnce()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);
            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async void ExchangeDeclareAsyncShouldThrowExceptionIfExchangeIsNull()
        {
            await _sut.ExchangeDeclareAsync(null);
        }
        #endregion

        #region PublishAsync
        [Test]
        public async Task PublishAsyncShouldPublishMessage()
        {
            var data = new Message<byte[]>(new byte[0]);

            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));
            _pipeline.Setup(x => x.OnSend(data))
                .Returns(new Message<byte[]>(new byte[0]));
            _serializationStrategy.Setup(x => x.Serialize(_message))
                .Returns(data);

            await _sut.PublishAsync(_exchange, RoutingKey, false, false, _message);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
            _pipeline.Verify(x => x.OnSend(data), Times.Once);
            _serializationStrategy.Verify(x => x.Serialize(_message), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsyncShouldThrowExceptionIfExchangeIsNull()
        {
            await _sut.PublishAsync(null, RoutingKey, false, false, _message);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsyncShouldThrowExceptionIfRoutingKeyIsNull()
        {
            await _sut.PublishAsync(_exchange, null, false, false, _message);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsyncShouldThrowExceptionIfMessageIsNull()
        {
            await _sut.PublishAsync<string>(_exchange, RoutingKey, false, false, null);
        }
        #endregion

        #region QueueBindAsync
        [Test]
        public async Task QueueBindAsyncShouldBindQueue()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.QueueBindAsync(_exchange, _queue, RoutingKey);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QueueBindAsyncShouldThrowExceptionIfExchangeIsNull()
        {
            await _sut.QueueBindAsync(null, _queue, RoutingKey);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QueueBindAsyncShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.QueueBindAsync(_exchange, null, RoutingKey);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QueueBindAsyncShouldThrowExceptionIfRoutingKeyIsNull()
        {
            await _sut.QueueBindAsync(_exchange, _queue, null);
        }
        #endregion

        #region QueueDeclareAsync
        [Test]
        public async Task QueueDeclareAsyncShouldDeclareQueue()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.QueueDeclareAsync(_queue);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldDeclareQueuePassive()
        {
            _queue.Passive = true;

            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.QueueDeclareAsync(_queue);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldSetArgs()
        {
            await Task.FromResult(0);

            Assert.Inconclusive();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QueueDeclareAsyncShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.QueueDeclareAsync(null);
        }
        #endregion

        [Test]
        public void ShouldDispose()
        {
            _commandDispatcher.Setup(x => x.Dispose());
            _connection.Setup(x => x.Dispose());

            _sut.Dispose();

            _commandDispatcher.Verify(x => x.Dispose(), Times.Once);
            _connection.Verify(x => x.Dispose(), Times.Once);
        }
    }
}