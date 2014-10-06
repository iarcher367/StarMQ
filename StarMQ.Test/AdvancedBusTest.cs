#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Test
{
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AdvancedBusTest
    {
        private const string RoutingKey = "x.y";

        private Mock<IConsumerFactory> _consumerFactory;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<IPublisher> _publisher;
        private IAdvancedBus _sut;

        private Action<IModel> _action;
        private IDictionary<string, object> _args;
        private Exchange _exchange;
        private IMessage<string> _message;
        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _consumerFactory = new Mock<IConsumerFactory>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _publisher = new Mock<IPublisher>();

            _sut = new AdvancedBus(_consumerFactory.Object, _dispatcher.Object, _log.Object,
                _publisher.Object);

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action<IModel>>()))
                .Callback<Action<IModel>>(x => _action = x)
                .Returns(Task.FromResult(0));

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

            _sut.BasicReturn += (o, e) => { flag = true; };

            _publisher.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs());

            Assert.That(flag, Is.True);
        }

        #region ConsumeAsync
        [Test]
        public void ConsumeShouldConsume()
        {
            Action<IHandlerRegistrar> configure = x => { };

            var consumer = new Mock<IConsumer>();

            _consumerFactory.Setup(x => x.CreateConsumer(false)).Returns(consumer.Object);

            _sut.ConsumeAsync(_queue, configure);

            _consumerFactory.Verify(x => x.CreateConsumer(false), Times.Once);
            consumer.Verify(x => x.Consume(_queue, configure, null), Times.Once);
        }

        [Test]
        public void ConsumeShouldReferenceExclusive()
        {
            _queue.WithExclusive(true).WithDurable(false);

            _sut.ConsumeAsync(_queue, null);

            _consumerFactory.Verify(x => x.CreateConsumer(_queue.Exclusive), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ConsumeShouldThrowExceptionIfQueueIsNull()
        {
            await _sut.ConsumeAsync(null, x => x.Add<string>((y, z) => new AckResponse()));
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
            Action action = () => { };
            Action<IModel, IBasicProperties, byte[]> publishAction = (x, y, z) => { };

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Callback<Action>(x => action = x)
                .Returns(Task.FromResult(0));
            _publisher.Setup(x => x.Publish(_message, It.IsAny<Action<IModel, IBasicProperties, byte[]>>()))
                .Callback<IMessage<string>, Action<IModel, IBasicProperties, byte[]>>(
                    (a, x) => publishAction = x)
                .Returns(Task.FromResult(0));

            await _sut.PublishAsync(_exchange, RoutingKey, false, false, _message);

            action();
            publishAction(_model.Object, null, null);

            _model.Verify(x => x.BasicPublish(_exchange.Name, RoutingKey, false, false,
                It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
            _publisher.Verify(x => x.Publish(_message,
                It.IsAny<Action<IModel, IBasicProperties, byte[]>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldSetMandatory()
        {
            const bool mandatory = true;
            Action action = () => { };
            Action<IModel, IBasicProperties, byte[]> publishAction = (x, y, z) => { };

            _dispatcher.Setup(x => x.Invoke(It.IsAny<Action>()))
                .Callback<Action>(x => action = x)
                .Returns(Task.FromResult(0));
            _publisher.Setup(x => x.Publish(_message, It.IsAny<Action<IModel, IBasicProperties, byte[]>>()))
                .Callback<IMessage<string>, Action<IModel, IBasicProperties, byte[]>>(
                    (a, x) => publishAction = x)
                .Returns(Task.FromResult(0));

            await _sut.PublishAsync(_exchange, RoutingKey, mandatory, false, _message);

            action();
            publishAction(_model.Object, null, null);

            _model.Verify(x => x.BasicPublish(_exchange.Name, RoutingKey, mandatory, false,
                It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
        }

        [Ignore("Immediate is not supported by RabbitMQ AMQP 0-9-1")]
        [Test]
        public async Task PublishAsyncShouldSetImmediate()
        {
            const bool immediate = true;
            Action<IModel, IBasicProperties, byte[]> publishAction = (x, y, z) => { };

            _publisher.Setup(x => x.Publish(_message, It.IsAny<Action<IModel, IBasicProperties, byte[]>>()))
                .Callback<IMessage<string>, Action<IModel, IBasicProperties, byte[]>>(
                    (a, x) => publishAction = x);

            await _sut.PublishAsync(_exchange, RoutingKey, false, immediate, _message);

            publishAction(_model.Object, null, null);

            _model.Verify(x => x.BasicPublish(_exchange.Name, RoutingKey, false, immediate,
                It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()), Times.Once);
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

            _dispatcher.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ShouldNotDisposeMultipleTimes()
        {
            _sut.Dispose();
            _sut.Dispose();

            _dispatcher.Verify(x => x.Dispose(), Times.Once);
        }
    }
}