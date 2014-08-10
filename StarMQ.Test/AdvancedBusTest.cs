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

    public class AdvancedBusTest    // TODO: wip
    {
        private const string RoutingKey = "x.y";

        private Mock<ICommandDispatcher> _commandDispatcher;
        private Mock<ILog> _log;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPublisher> _publisher;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private IAdvancedBus _sut;

        private Exchange _exchange;
        private IMessage<string> _message;

        [SetUp]
        public void Setup()
        {
            _commandDispatcher = new Mock<ICommandDispatcher>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);
            _publisher = new Mock<IPublisher>(MockBehavior.Strict);
            _serializationStrategy = new Mock<ISerializationStrategy>(MockBehavior.Strict);

            _sut = new AdvancedBus(_commandDispatcher.Object, _log.Object, _namingStrategy.Object,
                _publisher.Object, _serializationStrategy.Object);

            _exchange = new Exchange("StarMQ.Master");
            _message = new Message<string>("Hello World!");
        }

        [Test]
        public void ShouldConsume()
        {
            Assert.Fail();
        }

        #region ExchangeDeclare
        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclareExchange()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once());
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclarePassive()
        {
            _exchange.Passive = true;

            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once());
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldOnlyDeclareOnce()
        {
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));

            await _sut.ExchangeDeclareAsync(_exchange);
            await _sut.ExchangeDeclareAsync(_exchange);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once());
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
            _commandDispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0));
            _serializationStrategy.Setup(x => x.Serialize(_message))
                .Returns(new Message<byte[]>(new byte[0]));

            await _sut.PublishAsync(_exchange, RoutingKey, false, false, _message);

            _commandDispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once());
            _serializationStrategy.Verify(x => x.Serialize(_message), Times.Once());
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

        [Test]
        public void ShouldQueueDeclare()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldDisposeCommandDispatcher()
        {
            _commandDispatcher.Setup(x => x.Dispose());

            _sut.Dispose();

            _commandDispatcher.Verify(x => x.Dispose(), Times.Once());
        }
    }
}