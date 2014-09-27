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

namespace StarMQ.Test.Consume
{
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Consume;
    using StarMQ.Model;
    using System;
    using IConnection = StarMQ.Core.IConnection;

    public class PersistentConsumerDecoratorTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";

        private Mock<IConnection> _connection;
        private Mock<IConsumer> _consumer;
        private IConsumer _sut;

        private Queue _queue;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _consumer = new Mock<IConsumer>();

            _sut = new PersistentConsumerDecorator(_consumer.Object, _connection.Object);

            _queue = new Queue();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldReturnConsumerModel()
        {
            var model = new Mock<IModel>();

            _consumer.Setup(x => x.Model).Returns(model.Object);

            var actual = _sut.Model;

            Assert.That(actual, Is.SameAs(model.Object));

            _consumer.Verify(x => x.Model, Times.Once);
        }

        [Test]
        public void ShouldFireConsumerCancelledAndConsume()
        {
            var flag = false;

            _sut.Consume(_queue);
            _sut.ConsumerCancelled += (o, e) => flag = true;

            _consumer.Raise(x => x.ConsumerCancelled += null, new ConsumerEventArgs(String.Empty));

            Assert.That(flag, Is.True);

            _consumer.Verify(x => x.Consume(_queue, null, It.IsAny<PersistentConsumerDecorator>()),
                Times.Exactly(2));
        }

        [Test]
        public void ShouldConsumeOnConnectedEvent()
        {
            _sut.Consume(_queue);

            _connection.Raise(x => x.OnConnected += null);

            _consumer.Verify(x => x.Consume(_queue, null, It.IsAny<PersistentConsumerDecorator>()));
        }

        [Test]
        public void ShouldConsume()
        {
            Action<IHandlerRegistrar> configure = x => { };

            _sut.Consume(_queue, configure);

            _consumer.Verify(x => x.Consume(_queue, configure, It.IsAny<PersistentConsumerDecorator>()),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfQueueIsNull()
        {
            _sut.Consume(null);
        }

        [Test]
        public void ShouldBasicCancel()
        {
            _sut.HandleBasicCancel(ConsumerTag);

            _consumer.Verify(x => x.HandleBasicCancel(ConsumerTag), Times.Once);
        }

        [Test]
        public void ShouldBasicCancelOk()
        {
            _sut.HandleBasicCancelOk(ConsumerTag);

            _consumer.Verify(x => x.HandleBasicCancelOk(ConsumerTag), Times.Once);
        }

        [Test]
        public void ShouldBasicConsumeOk()
        {
            _sut.HandleBasicConsumeOk(ConsumerTag);

            _consumer.Verify(x => x.HandleBasicConsumeOk(ConsumerTag), Times.Once);
        }

        [Test]
        public void ShouldBasicDeliver()
        {
            const string exchange = "StarMQ.Master";
            const string routingKey = "#";
            var properties = new Mock<IBasicProperties>();

            _sut.HandleBasicDeliver(ConsumerTag, 0, true, exchange, routingKey, properties.Object,
                new byte[7]);

            _consumer.Verify(x => x.HandleBasicDeliver(ConsumerTag, 0, true, exchange, routingKey,
                properties.Object, new byte[7]), Times.Once);
        }

        [Test]
        public void ShouldModelShutdown()
        {
            var model = new Mock<IModel>();
            var args = new ShutdownEventArgs(ShutdownInitiator.Application, 0, String.Empty);

            _sut.HandleModelShutdown(model.Object, args);

            _consumer.Verify(x => x.HandleModelShutdown(model.Object, args));
        }

        [Test]
        public void ShouldDispose()
        {
            _sut.Dispose();

            _consumer.Verify(x => x.Dispose(), Times.Once);
        }
    }
}