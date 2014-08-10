namespace StarMQ.Test
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Model;
    using System;
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
            _advancedBus = new Mock<IAdvancedBus>();
            _configuration = new Mock<IConnectionConfiguration>();
            _namingStrategy = new Mock<INamingStrategy>();

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

        [Test]
        public void ShouldSubscribeAction()
        {
            Assert.Fail();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldSubscribeActionThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync(String.Empty, (Action<string>) null);
        }

        [Test]
        public void ShouldSubscribeFunc()
        {
            Assert.Fail();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldSubscribeFuncThrowExceptionIfSubscriptionIdIsNull()
        {
            await _sut.SubscribeAsync<string>(null, x => new Response());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldSubscribeFuncThrowExceptionIfMessageHandlerIsNull()
        {
            await _sut.SubscribeAsync(String.Empty, (Func<string, Response>) null);
        }

        [Test]
        public void ShouldDisposeAdvancedBus()
        {
            _sut.Dispose();

            _advancedBus.Verify(x => x.Dispose(), Times.Once());
        }
    }
}