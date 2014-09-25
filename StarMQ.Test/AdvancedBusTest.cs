namespace StarMQ.Test
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class AdvancedBusTest
    {
        private const string RoutingKey = "x.y";

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPipeline> _pipeline;
        private Mock<IPublisher> _publisher;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private IAdvancedBus _sut;

        private Action<IModel> _action;
        private IDictionary<string, object> _args;
        private Exchange _exchange;
        private IMessage<string> _message;
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
            _pipeline = new Mock<IPipeline>();
            _publisher = new Mock<IPublisher>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _sut = new AdvancedBus(_configuration.Object, _connection.Object,
                _dispatcher.Object, _log.Object, _namingStrategy.Object, _pipeline.Object,
                _publisher.Object, _serializationStrategy.Object);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Returns(Task.FromResult(0))
                .Callback<Action<IModel>>(x => _action = x);

            _action = x => { };
            _args = new Dictionary<string, object>();
            _exchange = new Exchange().WithName("StarMQ.Master");
            _message = new Message<string>("Hello World!");
            _queue = new Queue().WithName("StarMQ.Slave");
        }

        [Test]
        public void ShouldFireBasicReturnEventIfPublisherFiresBasicReturn()
        {
            var flag = false;
            var properties = new Mock<IBasicProperties>();

            _sut.BasicReturn += (o, e) => { flag = true; };

            _publisher.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = properties.Object
            });

            Assert.That(flag, Is.True);
        }

        #region ConsumeAsync
        [Test]
        public void ConsumeShouldConsume()
        {
            _sut.ConsumeAsync(_queue, x => x.Add<string>(y => new AckResponse()));

            Assert.Fail();  // TODO: implement once Consumer is mockable
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ConsumeShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.ConsumeAsync(null, x => x.Add<string>(y => new AckResponse()));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ConsumeShouldThrowExceptionIfConfigureIsNull()
        {
            await _sut.ConsumeAsync(_queue, null);
        }
        #endregion

        #region ExchangeDeclareAsync
        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclareExchange()
        {
            await _sut.ExchangeDeclareAsync(_exchange);

            _action(_model.Object);

            _model.Verify(x => x.ExchangeDeclare(_exchange.Name, _exchange.Type.ToString().ToLower(),
                _exchange.Durable, _exchange.AutoDelete, It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldDeclarePassive()
        {
            _exchange.Passive = true;

            await _sut.ExchangeDeclareAsync(_exchange);

            _action(_model.Object);

            _model.Verify(x => x.ExchangeDeclarePassive(_exchange.Name), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldNotSetArgsByDefault()
        {
            _model.Setup(x => x.ExchangeDeclare(_exchange.Name, _exchange.Type.ToString().ToLower(),
                _exchange.Durable, _exchange.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, string, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.ExchangeDeclareAsync(_exchange);

            _action(_model.Object);

            Assert.That(_args.Count, Is.EqualTo(0));

            _model.Verify(x => x.ExchangeDeclare(_exchange.Name, _exchange.Type.ToString().ToLower(),
                _exchange.Durable, _exchange.AutoDelete, It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldSetArgs()
        {
            const string key = "alternate-exchange";
            _exchange.AlternateExchangeName = "StarMQ";

            _model.Setup(x => x.ExchangeDeclare(_exchange.Name, _exchange.Type.ToString().ToLower(),
                _exchange.Durable, _exchange.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, string, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.ExchangeDeclareAsync(_exchange);

            _action(_model.Object);

            Assert.That(_args.ContainsKey(key), Is.True);
            Assert.That(_args[key], Is.EqualTo(_exchange.AlternateExchangeName));

            _model.Verify(x => x.ExchangeDeclare(_exchange.Name, _exchange.Type.ToString().ToLower(),
                _exchange.Durable, _exchange.AutoDelete, It.IsAny<Dictionary<string, object>>()),
                Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task ExchangeDeclareAsyncShouldOnlyDeclareOnce()
        {
            await _sut.ExchangeDeclareAsync(_exchange);
            await _sut.ExchangeDeclareAsync(_exchange);

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
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
            Action<IModel> publishAction = x => { };
            var properties = new Mock<IBasicProperties>();

            _model.Setup(x => x.CreateBasicProperties()).Returns(properties.Object);
            _pipeline.Setup(x => x.OnSend(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0]));
            _publisher.Setup(x => x.Publish(It.IsAny<Action<IModel>>()))
                .Callback<Action<IModel>>(x => publishAction = x);

            await _sut.PublishAsync(_exchange, RoutingKey, false, false, _message);

            _action(_model.Object);
            publishAction(_model.Object);

            _model.Verify(x => x.BasicPublish(_exchange.Name, RoutingKey, false, false,
                It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
            _pipeline.Verify(x => x.OnSend(It.IsAny<IMessage<byte[]>>()), Times.Once);
            _publisher.Verify(x => x.Publish(It.IsAny<Action<IModel>>()), Times.Once);
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
            await _sut.QueueBindAsync(_exchange, _queue, RoutingKey);

            _action(_model.Object);

            _model.Verify(x => x.QueueBind(_queue.Name, _exchange.Name, RoutingKey), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueBindAsyncShouldOnlyDeclareOnce()
        {
            await _sut.QueueBindAsync(_exchange, _queue, RoutingKey);
            await _sut.QueueBindAsync(_exchange, _queue, RoutingKey);

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
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
            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Returns(It.IsAny<QueueDeclareOk>());

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive, 
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldDeclareQueuePassive()
        {
            _queue.Passive = true;

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            _model.Verify(x => x.QueueDeclarePassive(_queue.Name), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldNotSetArgsByDefault()
        {
            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            Assert.That(_args.Count, Is.EqualTo(0));

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive, _queue.AutoDelete,
                It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldSetDeadLetterExchangeIfProvided()
        {
            const string key = "x-dead-letter-exchange";
            _queue.DeadLetterExchangeName = "StarMQ";

            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            Assert.That(_args.ContainsKey(key), Is.True);
            Assert.That(_args[key], Is.EqualTo(_queue.DeadLetterExchangeName));

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldSetDeadLetterRoutingKeyIfProvided()
        {
            const string key = "x-dead-letter-routing-key";
            _queue.DeadLetterRoutingKey = "StarMQ";

            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            Assert.That(_args.ContainsKey(key), Is.True);
            Assert.That(_args[key], Is.EqualTo(_queue.DeadLetterRoutingKey));

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldSetExpiresIfProvided()
        {
            const string key = "x-expires";
            _queue.Expires = 5;

            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            Assert.That(_args.ContainsKey(key), Is.True);
            Assert.That(_args[key], Is.EqualTo(_queue.Expires));

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldSetMessageTimeToLiveIfProvided()
        {
            const string key = "x-message-ttl";
            _queue.MessageTimeToLive = 42;

            _model.Setup(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()))
                .Callback<string, bool, bool, bool, IDictionary<string, object>>(
                    (a, b, c, d, x) => _args = x);

            await _sut.QueueDeclareAsync(_queue);

            _action(_model.Object);

            Assert.That(_args.ContainsKey(key), Is.True);
            Assert.That(_args[key], Is.EqualTo(_queue.MessageTimeToLive));

            _model.Verify(x => x.QueueDeclare(_queue.Name, _queue.Durable, _queue.Exclusive,
                _queue.AutoDelete, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
        }

        [Test]
        public async Task QueueDeclareAsyncShouldOnlyDeclareOnce()
        {
            await _sut.QueueDeclareAsync(_queue);
            await _sut.QueueDeclareAsync(_queue);

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action<IModel>>()), Times.Once);
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
            _sut.Dispose();

            _connection.Verify(x => x.Dispose(), Times.Once);
            _dispatcher.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ShouldNotDisposeMultipleTimes()
        {
            _sut.Dispose();
            _sut.Dispose();
            _sut.Dispose();

            _connection.Verify(x => x.Dispose(), Times.Once);
            _dispatcher.Verify(x => x.Dispose(), Times.Once);
        }
    }
}