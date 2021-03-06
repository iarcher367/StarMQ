﻿#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Test.Consume
{
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
        private IConsumer _sut;

        private Queue _queue;

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
                .Returns(_modelOne.Object)
                .Returns(_modelTwo.Object);
            _namingStrategy.SetupSequence(x => x.GetConsumerTag())
                .Returns(ConsumerTagOne)
                .Returns(ConsumerTagOne)
                .Returns(ConsumerTagTwo);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _handlerManager.Object, _log.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);

            _queue = new Queue().WithName("StarMQ.Slave");
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldApplyConfigure()
        {
            var configure = new Mock<Action<IHandlerRegistrar>>();

            _sut.Consume(_queue, configure.Object);

            configure.Verify(x => x(_handlerManager.Object), Times.Once());
        }

        [Test]
        public void ShouldValidateHandlerManager()
        {
            _sut.Consume(_queue);

            _handlerManager.Verify(x => x.Validate(), Times.Once);
        }

        [Test]
        public void ShouldOpenChannelOnFirstUse()
        {
            Action action = () => { };

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(_queue);

            action();

            _connection.Verify(x => x.CreateModel(), Times.Once);
        }

        [Test]
        public void ShouldNotOpenChannelIfAlreadyOpen()
        {
            Action action = () => { };

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);
            _modelOne.Setup(x => x.IsOpen).Returns(true);

            _sut.Consume(_queue);

            action();
            action();

            _connection.Verify(x => x.CreateModel(), Times.Once);
        }

        [Test]
        public void ShouldSetQosAndConsume()
        {
            const ushort prefetchCount = 10;
            Action action = () => { };

            _configuration.Setup(x => x.PrefetchCount).Returns(prefetchCount);
            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(_queue);

            action();

            _configuration.Verify(x => x.PrefetchCount, Times.Once);
            _connection.Verify(x => x.CreateModel(), Times.Once);
            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);
            _modelOne.Verify(x => x.BasicQos(0, prefetchCount, false), Times.Once);
            _modelOne.Verify(x => x.BasicConsume(_queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()), Times.Once);
        }

        [Test]
        public void ShouldOverrideConsumerTypeIfSet()
        {
            Action action = () => { };
            var decorator = new PersistentConsumerDecorator(_sut, _connection.Object);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(_queue, consumer: decorator);

            action();

            _modelOne.Verify(x => x.BasicConsume(_queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), decorator), Times.Once);
        }

        [Test]
        public void ShouldSetCancelOnHaFailoverIfSetInConfiguration()
        {
            const string key = "x-cancel-on-ha-failover";

            Action action = () => { };
            IDictionary<string, object> args = new Dictionary<string, object>();

            _configuration.Setup(x => x.CancelOnHaFailover).Returns(true);
            _modelOne.Setup(x => x.BasicConsume(_queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()))
                .Callback<string, bool, string, IDictionary<String, object>, IBasicConsumer>(
                    (a, b, c, x, d) => args = x);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(_queue);

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
            _queue.WithCancelOnHaFailover(true);

            _modelOne.Setup(x => x.BasicConsume(_queue.Name, false, It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<IConsumer>()))
                .Callback<string, bool, string, IDictionary<String, object>, IBasicConsumer>(
                    (a, b, c, x, d) => args = x);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>())).Callback<Action>(x => action = x);

            _sut.Consume(_queue);

            action();

            _dispatcher.Verify(x => x.Invoke(It.IsAny<Action>()), Times.Once);

            Assert.That(args.ContainsKey(key), Is.True);
            Assert.That(args[key], Is.EqualTo(_queue.CancelOnHaFailover));
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
                    Properties = new Properties { Type = typeof(Helper).FullName }
                });
            _serializationStrategy.Setup(x => x.Deserialize(It.IsAny<IMessage<byte[]>>(), typeof(Helper)))
                .Returns(new Message<dynamic>(new Helper())
                {
                    Properties = new Properties { Type = typeof(Helper).FullName }
                });

            IHandlerManager handlerOne = new HandlerManager(_log.Object);
            handlerOne.Add<Helper>((x, y) =>
            {
                Task.Delay(delay * 2).Wait();
                return new AckResponse();
            });
            var sutOne = new BasicConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, handlerOne, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object);
            var handlerTwo = new HandlerManager(_log.Object);
            handlerTwo.Add<Helper>((x, y) =>
            {
                Task.Delay(delay).Wait();
                return new AckResponse();
            });
            var sutTwo = new BasicConsumer(_configuration.Object, _connection.Object,
                _dispatcher.Object, handlerTwo, _log.Object, _namingStrategy.Object,
                _pipeline.Object, _serializationStrategy.Object);

            _modelOne.Setup(x => x.BasicAck(1, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(0)));
            _modelTwo.Setup(x => x.BasicAck(2, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(1)));
            _modelTwo.Setup(x => x.BasicAck(4, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(2)));
            _modelOne.Setup(x => x.BasicAck(3, false))
                .Callback(() => Assert.That(order++, Is.EqualTo(3)));

            await sutOne.Consume(new Queue().WithName("slowQueue"));
            await sutTwo.Consume(new Queue().WithName("fastQueue"));

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