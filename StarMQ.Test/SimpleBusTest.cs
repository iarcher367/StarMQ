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
    using Exception;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client.Events;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Threading.Tasks;

    public class SimpleBusTest
    {
        private const string Content = "Hello Earth!";
        private const string RoutingKey = "x.y";

        private Mock<IAdvancedBus> _advancedBus;
        private Mock<Action<Queue>> _configure;
        private Mock<INamingStrategy> _namingStrategy;
        private ISimpleBus _sut;

        [SetUp]
        public void Setup()
        {
            _advancedBus = new Mock<IAdvancedBus>();
            _configure = new Mock<Action<Queue>>();
            _namingStrategy = new Mock<INamingStrategy>();

            _sut = new SimpleBus(_advancedBus.Object, _namingStrategy.Object);

            _namingStrategy.Setup(x => x.GetExchangeName(typeof(string)))
                .Returns("StarMQ.Master");
            _namingStrategy.Setup(x => x.GetQueueName(typeof(string)))
                .Returns("StarMQ.Slave");
            _namingStrategy.Setup(x => x.GetDeadLetterName(It.IsAny<string>()))
                .Returns("DLX:StarMQ.Master");
            _namingStrategy.Setup(x => x.GetAlternateName(It.IsAny<Exchange>()))
                .Returns("AE:StarMQ.Master");
        }

        [Test]
        public void ShouldBasicReturn()
        {
            var flag = false;

            _sut.BasicReturn += (o, e) => flag = true;

            _advancedBus.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs());

            Assert.That(flag, Is.True);
        }

        #region PublishAsync
        [Test]
        public async Task PublishAsyncShouldPublish()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), String.Empty))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(), false, false, It.IsAny<Message<string>>()))
                .Returns(Task.FromResult(0));

            await _sut.PublishAsync(Content, RoutingKey);

            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Once);
            _namingStrategy.Verify(x => x.GetExchangeName(It.Is<Type>(y => y == typeof(string))), Times.Once);
            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(2));
            _advancedBus.Verify(x => x.QueueDeclareAsync(It.IsAny<Queue>()), Times.Once);
            _advancedBus.Verify(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(),
                String.Empty), Times.Once);
            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(), false, false,
                It.IsAny<Message<string>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldSetMandatory()
        {
            await _sut.PublishAsync(Content, RoutingKey, mandatory: true);

            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(),
                true, It.IsAny<bool>(), It.IsAny<Message<string>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldDefaultMandatoryToFalse()
        {
            await _sut.PublishAsync(Content, RoutingKey);

            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(),
                false, It.IsAny<bool>(), It.IsAny<Message<string>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldSetImmediate()
        {
            await _sut.PublishAsync(Content, RoutingKey, immediate: true);

            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(),
                It.IsAny<bool>(), true, It.IsAny<Message<string>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldDefaultImmediateToFalse()
        {
            await _sut.PublishAsync(Content, RoutingKey);

            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(),
                It.IsAny<bool>(), false, It.IsAny<Message<string>>()), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldApplyConfigureExchange()
        {
            var exchange = new Exchange();

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x => exchange = x);

            await _sut.PublishAsync(Content, RoutingKey, configure: x => x.WithAutoDelete(true));

            Assert.That(exchange.AutoDelete, Is.True);
        }

        [Test]
        public async Task PublishAsyncShouldUseConfiguredExchangeName()
        {
            const string expected = "StarMQ.Master";
            var flag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == expected)
                        flag = true;
                });

            await _sut.PublishAsync(Content, RoutingKey, configure: x => x.WithName(expected));

            Assert.That(flag, Is.True);

            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Never);
        }

        [Test]
        public async Task PublishAsyncShouldSetDefaultExchangeName()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.PublishAsync(Content, RoutingKey);

            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
        }

        [Test]
        public async Task PublishAsyncShouldUseConfiguredAlternateExchangeName()
        {
            const string expected = "AE:StarMQ.Master";
            var flag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == expected)
                        flag = true;
                });

            await _sut.PublishAsync(Content, RoutingKey,
                configure: x => x.WithAlternateExchangeName(expected));

            Assert.That(flag, Is.True);

            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Never);
        }

        [Test]
        public async Task PublishAsyncShouldSetDefaultAlternateExchangeName()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.PublishAsync(Content, RoutingKey);

            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsyncShouldThrowExceptionIfContentIsNull()
        {
            await _sut.PublishAsync<string>(null, RoutingKey);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PublishAsyncShouldThrowExceptionIfRoutingKeyIsNull()
        {
            await _sut.PublishAsync(Content, null);
        }
        #endregion

        #region SubscribeAsync
        [Test]
        public async Task SubscribeAsyncShouldSubscribe()
        {
            RunSubscribeSetup();

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            RunSubscribeVerify();
        }

        private void RunSubscribeSetup()
        {
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
        }

        private void RunSubscribeVerify()
        {
            _advancedBus.Verify(x =>
                x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()), Times.Once);
            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(3));
            _advancedBus.Verify(x => x.QueueDeclareAsync(It.IsAny<Queue>()), Times.Exactly(3));
            _advancedBus.Verify(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(),
                It.IsAny<string>()), Times.Exactly(3));

            _configure.Verify(x => x(It.IsAny<Queue>()), Times.Never);

            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Once);
            _namingStrategy.Verify(x => x.GetDeadLetterName(It.IsAny<string>()), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetQueueName(typeof(string)), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncShouldThrowExceptionIfHandlerRegistrarIsNull()
        {
            await _sut.SubscribeAsync(null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public async Task SubscribeAsyncShouldThrowExceptionIfNoHandlersRegistered()
        {
            await _sut.SubscribeAsync(x => { });
        }

        [Test]
        public async Task SubscribeAsyncShouldApplyConfigureExchange()
        {
            var flag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.AutoDelete)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()), null,
                x => x.WithAutoDelete(true));

            Assert.That(flag, Is.True);
        }

        [Test]
        public async Task SubscribeAsyncShouldUseConfiguredExchangeName()
        {
            const string expected = "StarMQ.Master";
            var flag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == expected)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()), null,
                x => x.WithName(expected));

            Assert.That(flag, Is.True);

            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(3));
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Never);
        }

        [Test]
        public async Task SubscribeAsyncShouldSetDefaultExchangeName()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
        }

        [Test]
        public async Task SubscribeAsyncShouldUseConfiguredAlternateExchangeName()
        {
            const string expected = "AE:StarMQ.Master";
            var flag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == expected)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()), null,
                x => x.WithAlternateExchangeName(expected));

            Assert.That(flag, Is.True);

            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(3));
            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Never);
        }

        [Test]
        public async Task SubscribeAsyncShouldSetDefaultAlternateExchangeName()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            _namingStrategy.Verify(x => x.GetAlternateName(It.IsAny<Exchange>()), Times.Once);
        }

        [Test]
        public async Task SubscribeAsyncShouldApplyConfigureQueue()
        {
            var flag = false;

            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue>(x =>
                {
                    if (x.CancelOnHaFailover && x.Exclusive)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => { }),
                x => x.WithCancelOnHaFailover(true)
                        .WithExclusive(true));

            Assert.That(flag, Is.True);
        }

        [Test]
        public async Task SubscribeAsyncShouldUseConfiguredQueueName()
        {
            const string expected = "StarMQ.Master";
            var declareFlag = false;
            var bindFlag = false;
            var consumeFlag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue, Action<IHandlerRegistrar>>((x, a) => consumeFlag = x.Name == expected);
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue>(x =>
                {
                    if (x.Name == expected)
                        declareFlag = true;
                });
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange, Queue, string>((a, x, b) =>
                {
                    if (x.Name == expected)
                        bindFlag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()),
                x => x.WithName(expected).WithBindingKey("StarMQ.#"));

            Assert.That(declareFlag, Is.True);
            Assert.That(bindFlag, Is.True);
            Assert.That(consumeFlag, Is.True);

            _namingStrategy.Verify(x => x.GetQueueName(It.IsAny<Type>()), Times.Never);
        }

        [Test]
        public async Task SubscribeAsyncShouldSetDefaultQueueName()
        {
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue, Action<IHandlerRegistrar>>((x, a) =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange, Queue, string>((a, x, b) =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            _namingStrategy.Verify(x => x.GetQueueName(It.IsAny<Type>()), Times.Once);
        }

        [Test]
        public async Task SubscribeAsyncShouldBindDefaultKeyIfNoBindingKeysSet()
        {
            var flag = false;

            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange, Queue, string>((a, b, x) =>
                {
                    if (x == "#")
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            Assert.That(flag, Is.True);
        }

        [Test]
        public async Task SubscribeAsyncShouldUseConfiguredDeadLetterExchangeName()
        {
            const string expected = "DLX:StarMQ.Master";
            var flag = false;

            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == expected)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()),
                x => x.WithDeadLetterExchangeName(expected));

            Assert.That(flag, Is.True);

            _namingStrategy.Verify(x => x.GetDeadLetterName(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task SubscribeAsyncShouldSetDefaultDeadLetterExchangeName()
        {
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange>(x =>
                {
                    if (x.Name == null)
                        Assert.Fail();
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            _namingStrategy.Verify(x => x.GetDeadLetterName(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task SubscribeAsyncShouldSetDefaultDeadLetterQueueName()
        {
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue>(x =>
                    {
                        if (x.Name == null)
                            Assert.Fail();
                    });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()));

            _namingStrategy.Verify(x => x.GetDeadLetterName(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task SubscribeAsyncShouldUseSameBindingKeysforDeadLetterQueueIfNotSet()
        {
            const string expected = "alpha.*";
            var count = 0;

            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange, Queue, string>((a, b, x) =>
                    {
                        if (x == expected)
                            count++;
                    });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()),
                x => x.WithBindingKey(expected));

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task SubscribeAsyncShouldBindDeadLetterRoutingKeyToDeadLetterQueue()
        {
            const string expected = "alpha.beta";
            var flag = false;

            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Action<IHandlerRegistrar>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0))
                .Callback<Exchange, Queue, string>((a, b, x) =>
                {
                    if (x == expected)
                        flag = true;
                });

            await _sut.SubscribeAsync(x => x.Add<string>(y => new AckResponse()),
                x => x.WithDeadLetterRoutingKey(expected));

            Assert.That(flag, Is.True);
        }
        #endregion

        [Test]
        public void ShouldDisposeAdvancedBus()
        {
            _advancedBus.Setup(x => x.Dispose());

            _sut.Dispose();

            _advancedBus.Verify(x => x.Dispose(), Times.Once);
        }
    }
}