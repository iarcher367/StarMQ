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
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BaseConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const ulong DeliveryTag = 42;
        private const ushort PrefetchCount = 10;

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IInboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IBasicProperties> _properties;
        private BaseConsumer _sut;

        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _connection = new Mock<IConnection>(MockBehavior.Strict);
            _dispatcher = new Mock<IInboundDispatcher>(MockBehavior.Strict);
            _log = new Mock<ILog>();
            _model = new Mock<IModel>(MockBehavior.Strict);
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);
            _properties = new Mock<IBasicProperties>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _log.Object, _namingStrategy.Object);

            _queue = new Queue("StarMQ.Slave");
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Verify(x => x.CreateModel(), Times.Once);
            _namingStrategy.Verify(x => x.GetConsumerTag(), Times.Once);
        }

        [Test]
        public void CancelShouldFireConsumerCancelledEvent()
        {
            var flag = false;

            _sut.ConsumerCancelled += (o, e) => flag = true;

            _sut.HandleBasicCancel(ConsumerTag);

            Assert.That(flag, Is.True);
        }

        [Test]
        public void CancelOkShouldDispose()
        {
            _model.Setup(x => x.Dispose());

            _sut.HandleBasicCancelOk(ConsumerTag);

            _model.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ConsumeOkShouldSetConsumerTag()
        {
            _sut.HandleBasicConsumeOk(ConsumerTag);

            Assert.That(_sut.ConsumerTag, Is.EqualTo(ConsumerTag));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConsumeOkShouldThrowExceptionIfConsumerTagIsNull()
        {
            _sut.HandleBasicConsumeOk(null);
        }

        [Test]
        public void DeliverShouldQueueMessageToDispatcher()
        {
            var properties = new Mock<IBasicProperties>();
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Returns(Task.FromResult(0));

            _sut.Consume(new Queue(String.Empty), x => new AckResponse());
            _sut.HandleBasicDeliver(ConsumerTag, 0, false, String.Empty, String.Empty,
                properties.Object, new byte[0]);

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
        }

        [Test]
        public async Task DeliverShouldBasicCancelOnUnsubscribeAction()
        {
            Action action = () => { };

            DeliverConsumeSetup();

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Returns(Task.FromResult(0))
                .Callback<Action>(x => action = x);
            _model.Setup(x => x.BasicAck(DeliveryTag, false));
            _model.Setup(x => x.BasicCancel(ConsumerTag));

            await _sut.Consume(_queue, x => new AckResponse
            {
                Action = ResponseAction.Unsubscribe
            });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            action();

            DeliverConsumeVerify();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
            _model.Verify(x => x.BasicCancel(ConsumerTag), Times.Once);
        }

        private void DeliverConsumeSetup()
        {
            _configuration.Setup(x => x.PrefetchCount).Returns(PrefetchCount);
            _model.Setup(x => x.BasicConsume(_queue.Name, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), _sut))
                .Returns(String.Empty);
            _model.Setup(x => x.BasicQos(0, PrefetchCount, false));
            _model.Setup(x => x.IsOpen).Returns(true);
        }

        private void DeliverConsumeVerify()
        {
            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _model.Verify(x => x.BasicConsume(_queue.Name, false, ConsumerTag,
                It.IsAny<Dictionary<string, object>>(), _sut), Times.Once);
            _model.Verify(x => x.BasicQos(0, PrefetchCount, false), Times.Once);
            _model.Verify(x => x.IsOpen, Times.Once);
        }

        [Test]
        public async Task DeliverShouldBasicAckWithDeliveryTag()
        {
            Action action = () => { };

            DeliverConsumeSetup();

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Returns(Task.FromResult(0))
                .Callback<Action>(x => action = x);
            _model.Setup(x => x.BasicAck(DeliveryTag, false));

            await _sut.Consume(_queue, x => new AckResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            action();

            DeliverConsumeVerify();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
        }

        [Test]
        public async Task DeliverShouldBasicNackWithDeliveryTag()
        {
            Action action = () => { };

            DeliverConsumeSetup();

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Returns(Task.FromResult(0))
                .Callback<Action>(x => action = x);
            _model.Setup(x => x.BasicNack(DeliveryTag, false, false));

            await _sut.Consume(_queue, x => new NackResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            action();

            DeliverConsumeVerify();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
            _model.Verify(x => x.BasicNack(DeliveryTag, false, false), Times.Once);
        }

        [Test]
        public void ModelShutdownShouldDoNothing()
        {
            _sut.HandleModelShutdown(It.IsAny<IModel>(),
                new ShutdownEventArgs(ShutdownInitiator.Application, 0, String.Empty));
        }

        [Test]
        public void ShouldDispose()
        {
            _model.Setup(x => x.Dispose());

            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}