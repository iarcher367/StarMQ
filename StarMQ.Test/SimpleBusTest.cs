namespace StarMQ.Test
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SimpleBusTest      // TODO: wip
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
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(), false, false, It.IsAny<Message<string>>()))
                .Returns(Task.FromResult(0));

            await _sut.PublishAsync(Content, RoutingKey);

            _namingStrategy.Verify(x => x.GetExchangeName(It.Is<Type>(y => y == typeof(string))), Times.Once());
            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Once());
            _advancedBus.Verify(x => x.PublishAsync(It.IsAny<Exchange>(), It.IsAny<string>(), false, false,
                It.IsAny<Message<string>>()), Times.Once());
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

            await _sut.SubscribeAsync<string>(String.Empty, new List<string>(), x => new Response());

            RunSubscribeVerify();
        }

        private void RunSubscribeSetup()
        {
            _advancedBus.Setup(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, Response>>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x => x.QueueDeclareAsync(It.IsAny<Queue>()))
                .Returns(Task.FromResult(0));
            _advancedBus.Setup(x =>
                x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            _namingStrategy.Setup(x => x.GetDeadLetterExchangeName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetExchangeName(typeof(string)))
                .Returns(String.Empty);
            _namingStrategy.Setup(x => x.GetQueueName(typeof(string), String.Empty))
                .Returns(String.Empty);
        }

        private void RunSubscribeVerify()
        {
            _advancedBus.Verify(x => x.ConsumeAsync(It.IsAny<Queue>(), It.IsAny<Func<string, Response>>())
                , Times.Once);
            _advancedBus.Verify(x => x.ExchangeDeclareAsync(It.IsAny<Exchange>()), Times.Once);
            _advancedBus.Verify(x => x.QueueDeclareAsync(It.IsAny<Queue>()), Times.Once);
            _advancedBus.Verify(x => x.QueueBindAsync(It.IsAny<Exchange>(), It.IsAny<Queue>(),
                                                        It.IsAny<string>()), Times.Once());
            _namingStrategy.Verify(x => x.GetDeadLetterExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetExchangeName(typeof(string)), Times.Once);
            _namingStrategy.Verify(x => x.GetQueueName(typeof(string), String.Empty), Times.Once);
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

            _sut.SubscribeAsync<string>(String.Empty, new List<string>(), x => new Response());

            RunSubscribeVerify();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfSubscriptionIdIsNull()
        {
            await _sut.SubscribeAsync<string>(null, new List<string>(), x => new Response());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfRoutingKeysAreNull()
        {
            await _sut.SubscribeAsync<string>(String.Empty, null, x => new Response());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SubscribeAsyncFuncShouldThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync(String.Empty, new List<string>(), (Func<string, Response>)null);
        }
        #endregion

        [Test]
        public void ShouldDisposeAdvancedBus()
        {
            _advancedBus.Setup(x => x.Dispose());

            _sut.Dispose();

            _advancedBus.Verify(x => x.Dispose(), Times.Once());
        }
    }
}