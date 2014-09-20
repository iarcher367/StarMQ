namespace StarMQ.Test.Consume
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BaseConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const int Delay = 10;
        private const ulong DeliveryTag = 42;

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IBasicProperties> _properties;
        private BaseConsumer _sut;

        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>();
            _properties = new Mock<IBasicProperties>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _log.Object, _namingStrategy.Object);

            _queue = new Queue();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldDisposeModelAndDiscardMessagesIfOnDisconnectedFires()
        {
            _connection.Raise(x => x.OnDisconnected += null);

            _model.Verify(x => x.Dispose(), Times.Once);

            Assert.Inconclusive();
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
        public void CancelOkShouldDoNothing()
        {
            _sut.HandleBasicCancelOk(ConsumerTag);
        }

        [Test]
        public void ConsumeOkShouldSetConsumerTag()
        {
            var tag = Guid.NewGuid().ToString();

            _sut.HandleBasicConsumeOk(tag);

            Assert.That(_sut.ConsumerTag, Is.EqualTo(tag));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConsumeOkShouldThrowExceptionIfConsumerTagIsNull()
        {
            _sut.HandleBasicConsumeOk(null);
        }

        [Test]
        public async Task DeliverShouldExecuteMessageHandler()
        {
            var signal = new ManualResetEventSlim(false);

            await _sut.Consume(_queue, x =>
            {
                signal.Set();
                return new AckResponse();
            });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            signal.Wait();
        }

        [Test]
        public async Task DeliverShouldNotBeginProcessingIfCancelled()
        {
            var count = 0;

            await _sut.Consume(_queue, x =>
                {
                    count += 2;
                    return new AckResponse();
                });

            _sut.Dispose();

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);

            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public async Task DeliverShouldBasicAckWithDeliveryTag()
        {
            await _sut.Consume(_queue, x => new AckResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);

            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
        }

        [Test]
        public async Task DeliverShouldBasicNackWithDeliveryTag()
        {
            await _sut.Consume(_queue, x => new NackResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);

            _model.Verify(x => x.BasicNack(DeliveryTag, false, false), Times.Once);
        }

        [Test]
        public async Task DeliverShouldBasicCancelOnUnsubscribeAction()
        {
            await _sut.Consume(_queue, x => new AckResponse { Action = ResponseAction.Unsubscribe });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);

            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
            _model.Verify(x => x.BasicCancel(ConsumerTag), Times.Once);
            _model.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public async Task DeliverShouldDoNothingOnModelAlreadyClosedException()
        {
            _model.Setup(x => x.BasicAck(DeliveryTag, false)).Throws(new AlreadyClosedException(null));

            await _sut.Consume(_queue, x => new AckResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);
        }

        [Test]
        public async Task DeliverShouldDoNothingOnModelNotSupportedException()
        {
            _model.Setup(x => x.BasicAck(DeliveryTag, false)).Throws(new NotSupportedException(null));

            await _sut.Consume(_queue, x => new AckResponse());

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            await Task.Delay(Delay);
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
            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}