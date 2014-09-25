﻿namespace StarMQ.Test.Consume
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
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BasicConsumerTest
    {
        private const string ConsumerTagOne = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const string ConsumerTagTwo = "cb5239f7-80c6-48ec-a5ed-aa1a6b73e4f2";

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
        private Mock<ITypeNameSerializer> _typeNameSerializer;
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
            _typeNameSerializer = new Mock<ITypeNameSerializer>();

            _connection.SetupSequence(x => x.CreateModel())
                .Returns(_modelOne.Object)
                .Returns(_modelOne.Object)
                .Returns(_modelTwo.Object);
            _namingStrategy.SetupSequence(x => x.GetConsumerTag())
                .Returns(ConsumerTagOne)
                .Returns(ConsumerTagOne)
                .Returns(ConsumerTagTwo);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, _handlerManager.Object, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object, _typeNameSerializer.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldSetQosAndConsume()
        {
            const ushort prefetchCount = 10;
            Action action = () => { };
            var queue = new Queue().WithName(String.Empty);

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            action();

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
            _modelOne.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Once);
            _modelOne.Verify(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Once);
        }

        [Test]
        public void ShouldSetCancelOnHaFailoverIfSetInConfiguration()
        {
            const string key = "x-cancel-on-ha-failover";

            Action action = () => { };
            IDictionary<string, object> args = new Dictionary<string, object>();
            var queue = new Queue().WithName(String.Empty);

            _configuration.Setup(x => x.CancelOnHaFailover).Returns(true);
            _modelOne.Setup(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()))
                .Callback<string, bool, string, IDictionary<String, object>, IBasicConsumer>(
                    (a, b, c, x, d) => args = x);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            action();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);

            Assert.That(args.ContainsKey(key), Is.True);
            Assert.That(args[key], Is.True);
        }

        [Test]
        public void ShouldSetCancelOnHaFailoverForQueue()
        {
            const string key = "x-cancel-on-ha-failover";

            Action action = () => { };
            IDictionary<string, object> args = new Dictionary<string, object>();
            var queue = new Queue().WithName(String.Empty).WithCancelOnHaFailover(true);

            _modelOne.Setup(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()))
                .Callback<string, bool, string, IDictionary<String, object>, IBasicConsumer>(
                    (a, b, c, x, d) => args = x);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            action();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);

            Assert.That(args.ContainsKey(key), Is.True);
            Assert.That(args[key], Is.EqualTo(queue.CancelOnHaFailover));
        }

        [Test]
        public void ShouldSetPriority()
        {
            const string key = "x-priority";

            Action action = () => { };
            IDictionary<string, object> args = new Dictionary<string, object>();
            var queue = new Queue().WithName(String.Empty).WithPriority(7);

            _modelOne.Setup(x => x.BasicConsume(queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()))
                .Callback<string, bool, string, IDictionary<String, object>, IBasicConsumer>(
                    (a, b, c, x, d) => args = x);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(queue);

            action();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);

            Assert.That(args.ContainsKey(key), Is.True);
            Assert.That(args[key], Is.EqualTo(queue.Priority));
        }

        [Test]
        public async Task ShouldProcessQueuesIndependently()
        {
            const int delay = 10;
            var order = 0;

            var properties = new Mock<IBasicProperties>();
            _pipeline.Setup(x => x.OnReceive(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0])
                {
                    Properties = new Properties { Type = typeof(Factory).FullName }
                });
            _serializationStrategy.Setup(x => x.Deserialize(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<dynamic>(new Factory())
                {
                    Properties = new Properties { Type = typeof(Factory).FullName }
                });

            IHandlerManager handlerOne = new HandlerManager(_log.Object);
            handlerOne.Add<Factory>(x =>
            {
                Task.Delay(delay * 2).Wait();
                return new AckResponse();
            });
            var sutOne = new BasicConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, handlerOne, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object, _typeNameSerializer.Object);
            var handlerTwo = new HandlerManager(_log.Object);
            handlerTwo.Add<Factory>(x =>
            {
                Task.Delay(delay).Wait();
                return new AckResponse();
            });
            var sutTwo = new BasicConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, handlerTwo, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object, _typeNameSerializer.Object);

            _modelOne.Setup(x => x.BasicAck(1, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(0)));
            _modelTwo.Setup(x => x.BasicAck(2, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(1)));
            _modelTwo.Setup(x => x.BasicAck(4, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(2)));
            _modelOne.Setup(x => x.BasicAck(3, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(3)));

            sutOne.HandleBasicDeliver(ConsumerTagOne, 1, false, String.Empty, String.Empty,
                properties.Object, new byte[1]);
            sutTwo.HandleBasicDeliver(ConsumerTagTwo, 2, false, String.Empty, String.Empty,
                properties.Object, new byte[1]);
            sutOne.HandleBasicDeliver(ConsumerTagOne, 3, false, String.Empty, String.Empty,
                properties.Object, new byte[1]);
            sutTwo.HandleBasicDeliver(ConsumerTagTwo, 4, false, String.Empty, String.Empty,
                properties.Object, new byte[1]);

            await Task.Delay(delay * 4);

            sutOne.Dispose();
            sutTwo.Dispose();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.Consume(null);
        }
    }
}