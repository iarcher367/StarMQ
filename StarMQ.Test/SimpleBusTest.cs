namespace StarMQ.Test
{
    using Moq;
    using NUnit.Framework;
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
            _namingStrategy.Setup(x => x.GetDeadLetterExchangeName(typeof(string)))
                .Returns("DLX:StarMQ.Master");
            _namingStrategy.Setup(x => x.GetDeadLetterQueueName(typeof(string)))
                .Returns("DLX:StarMQ.Slave");
            _namingStrategy.Setup(x => x.GetAlternateExchangeName(typeof(string)))
                .Returns("AE:StarMQ.Master");
            _namingStrategy.Setup(x => x.GetAlternateQueueName(typeof(string)))
                .Returns("AE:StarMQ.Slave");
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

            _namingStrategy.Verify(x => x.GetAlternateExchangeName(It.Is<Type>(y => y == typeof(string))), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetAlternateQueueName(It.Is<Type>(y => y == typeof(string))), Times.Once);
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
            await _sut.PublishAsync(Content, RoutingKey, true);

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
        public async Task SubscribeAsyncActionShouldSubscribe()
        {
            RunSubscribeSetup();

            await _sut.SubscribeAsync<string>(x => new AckResponse());

            RunSubscribeVerify();
        }

        private void RunSubscribeSetup()
        {
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
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
                x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()),
                Times.Once);
            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Exactly(3));
            _advancedBus.Verify(x => x.QueueDeclareAsync(It.IsAny<Queue>()), Times.Exactly(3));
            _advancedBus.Verify(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(),
                It.IsAny<string>()), Times.Exactly(3));

            _configure.Verify(x => x(It.IsAny<Queue>()), Times.Never);

            _namingStrategy.Verify(x => x.GetAlternateExchangeName(typeof(string)), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetAlternateQueueName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetDeadLetterExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetDeadLetterQueueName(typeof(string)),
                Times.Once);
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetQueueName(typeof(string)), Times.Once);
        }

        [Test]
        public async Task SubscribeAsyncActionShouldReturnAckResponseIfNoException()
        {
            Func<string, BaseResponse> func = x => null;

            RunSubscribeSetup();

            _advancedBus.Setup(x =>
                x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue, Func<string, BaseResponse>>((_, x) => func = x);

            await _sut.SubscribeAsync<string>(x => { });

            var actual = func("StarMQ");

            Assert.That(actual, Is.TypeOf(typeof(AckResponse)));

            RunSubscribeVerify();
        }

        [Test]
        public async Task SubscribeAsyncActionShouldReturnNackResponseIfHandlerThrowsException()
        {
            Func<string, BaseResponse> func = x => null;

            RunSubscribeSetup();

            _advancedBus.Setup(x =>
                x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue, Func<string, BaseResponse>>((_, x) => func = x);

            await _sut.SubscribeAsync((Action<string>)
                (x => { throw new NotImplementedException(); }));

            var actual = func("StarMQ");

            Assert.That(actual, Is.TypeOf(typeof(NackResponse)));

            RunSubscribeVerify();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncActionShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync((Action<string>)null);
        }

        [Test]
        public async Task SubscribeAsyncFuncShouldSubscribe()
        {
            RunSubscribeSetup();

            await _sut.SubscribeAsync<string>(x => new AckResponse());

            RunSubscribeVerify();
        }

        [Test]
        public async Task SubscribeAsyncFuncShouldApplyConfigure()
        {
            var configure = new Mock<Action<Queue>>();

            await _sut.SubscribeAsync<string>(x => new AckResponse(), configure.Object);

            configure.Verify(x => x(It.IsAny<Queue>()), Times.Once());
        }

        [Test]
        public async Task SubscribeAsyncFuncShouldUseConfiguredQueueName()
        {
            const string expected = "StarMQ.Master";
            var declareFlag = false;
            var bindFlag = false;
            var consumeFlag = false;

            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
                .Returns(Task.FromResult(0))
                .Callback<Queue, Func<string, BaseResponse>>((x, a) => consumeFlag = x.Name == expected);
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

            await _sut.SubscribeAsync<string>(x => new AckResponse(), x => x.WithName(expected)
                .WithBindingKey("StarMQ.#"));

            Assert.That(declareFlag, Is.True);
            Assert.That(bindFlag, Is.True);
            Assert.That(consumeFlag, Is.True);

            _namingStrategy.Verify(x => x.GetQueueName(It.IsAny<Type>()), Times.Never);
        }

        [Test]
        public async Task SubscribeAsyncFuncShouldUseConfiguredDeadLetterExchangeName()
        {
            const string expected = "StarMQ";
            var flag = false;

            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
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

            await _sut.SubscribeAsync<string>(x => new AckResponse(),
                x => x.WithDeadLetterExchangeName(expected));

            Assert.That(flag, Is.True);

            _namingStrategy.Verify(x => x.GetDeadLetterExchangeName(It.IsAny<Type>()), Times.Never);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync((Func<string, BaseResponse>)null);
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