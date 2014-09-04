namespace StarMQ.Test
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SimpleBusTest
    {
        private const string Content = "Hello Earth!";
        private const string RoutingKey = "x.y";

        private Mock<IAdvancedBus> _advancedBus;
        private Mock<IConnectionConfiguration> _configuration;
        private Mock<INamingStrategy> _namingStrategy;
        private ISimpleBus _sut;

        [SetUp]
        public void Setup()
        {
            _advancedBus = new Mock<IAdvancedBus>(MockBehavior.Strict);
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
            _namingStrategy = new Mock<INamingStrategy>(MockBehavior.Strict);

            _sut = new SimpleBus(_advancedBus.Object, _configuration.Object, _namingStrategy.Object);
        }

        #region PublishAsync
        [Test]
        public async Task PublishAsyncShouldPublish()
        {
            _namingStrategy.Setup(x => x.GetExchangeName(It.Is<Type>(y => y == typeof(string))))
                .Returns("StarMQ.Master");
            _namingStrategy.Setup(x => x.GetAlternateExchangeName(It.Is<Type>(y => y == typeof(string))))
                .Returns("AE:StarMQ.Master");
            _namingStrategy.Setup(x => x.GetAlternateQueueName(It.Is<Type>(y => y == typeof(string))))
                .Returns("AE:StarMQ.Master");
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

            await _sut.SubscribeAsync<string>(String.Empty, new List<string>(), x => new AckResponse());

            RunSubscribeVerify();
        }

        private void RunSubscribeSetup()
        {
            _advancedBus.Setup(x =>
                x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, BaseResponse>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x =>
                x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _namingStrategy.Setup(x => x.GetAlternateExchangeName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetAlternateQueueName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetDeadLetterExchangeName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetDeadLetterQueueName(typeof(string), String.Empty))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetExchangeName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetQueueName(typeof(string), String.Empty))
                .Returns(String.Empty);
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
            _namingStrategy.Verify(x => x.GetAlternateExchangeName(typeof(string)), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetAlternateQueueName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetDeadLetterExchangeName(typeof(string)), Times.Exactly(2));
            _namingStrategy.Verify(x => x.GetDeadLetterQueueName(typeof(string), String.Empty),
                Times.Once);
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetQueueName(typeof(string), String.Empty), Times.Once);
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

            await _sut.SubscribeAsync<string>(String.Empty, new List<string>(), x => { });

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

            await _sut.SubscribeAsync(String.Empty, new List<string>(), (Action<string>)
                (x => { throw new NotImplementedException(); }));

            var actual = func("StarMQ");

            Assert.That(actual, Is.TypeOf(typeof(NackResponse)));

            RunSubscribeVerify();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncActionShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync(String.Empty, new List<string>(), (Action<string>)null);
        }

        [Test]
        public void SubscribeAsyncFuncShouldSubscribe()
        {
            RunSubscribeSetup();

            _sut.SubscribeAsync<string>(String.Empty, new List<string>(), x => new AckResponse());

            RunSubscribeVerify();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfSubscriptionIdIsNull()
        {
            await _sut.SubscribeAsync<string>(null, new List<string>(), x => new AckResponse());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfRoutingKeysAreNull()
        {
            await _sut.SubscribeAsync<string>(String.Empty, null, x => new AckResponse());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync(String.Empty, new List<string>(), (Func<string, BaseResponse>)null);
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