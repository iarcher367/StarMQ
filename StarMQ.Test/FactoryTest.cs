namespace StarMQ.Test
{
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;

    public class FactoryTest
    {
        private const double CompressionRatio = 0.7;
        private const string Content = "Lorem ipsum dolor sit amet, est ea dicit verear albucius. " +
            "Populo phaedrum efficiantur has no, usu ex decore accusamus, ad nec etiam mazim " +
            "constituto. Liber prompta reprehendunt ea sea, no case fierent qui, elit viderer an " +
            "vix. Eum ea harum veritus. Dicunt labitur quaestio eu nam. Vide graece democritum.";

        private Factory _sut;

        internal class EmptyStrategy : ICorrelationStrategy
        {
            public string GenerateCorrelationId() { return String.Empty; }
        }

        [SetUp]
        public void Setup()
        {
            _sut = new Factory();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldConfigureConnectionFactoryFromConnectionConfiguration()
        {
            var factory = new Factory().Container.GetInstance<ConnectionFactory>();

            Assert.That(factory.Port, Is.Not.EqualTo(-1));
            Assert.That(factory.RequestedHeartbeat, Is.Not.EqualTo(0));
        }

        [Test]
        public void ShouldSetClientPropertiesInConnectionFactory()
        {
            var factory = new Factory().Container.GetInstance<ConnectionFactory>();

            Assert.That(factory.ClientProperties.Count, Is.GreaterThan(5));
        }

        [Test]
        public void ShouldReturnBasicPublisherIfConfirmsDisabled()
        {
            var actual = _sut.Container.GetInstance<IPublisher>();

            Assert.That(actual, Is.TypeOf<BasicPublisher>());
        }

        [Test]
        public void ShouldReturnConfirmsPublisherIfConfirmsEnabled()
        {
            var configuration = _sut.Container.GetInstance<IConnectionConfiguration>();

            Global.ParseConfiguration(configuration, "publisherconfirms=true");

            var actual = _sut.Container.GetInstance<IPublisher>();

            Assert.That(actual, Is.TypeOf<ConfirmPublisher>());
        }

        [Test]
        public void ShouldAllowOverridingRegistrations()
        {
            _sut.Container.RegisterSingle<ICorrelationStrategy, EmptyStrategy>();

            var actual = _sut.Container.GetInstance<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<EmptyStrategy>());
        }

        [Test]
        public void ShouldAddInterceptor()
        {
            _sut.AddInterceptor(new CompressionInterceptor());

            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = _sut.Container.GetInstance<IPipeline>().OnSend(message).Body;

            Assert.That(body.Length, Is.LessThan(message.Body.Length * CompressionRatio));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddInterceptorShouldThrowExceptionIfInterceptorIsNull()
        {
            _sut.AddInterceptor(null);
        }

        [Test]
        public void ShouldEnableCompression()
        {
            var pipeline = _sut.EnableCompression()
                .Container.GetInstance<IPipeline>();
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = pipeline.OnSend(message).Body;

            Assert.That(body.Length, Is.LessThan(message.Body.Length * CompressionRatio));
        }

        [Test]
        public void ShouldEnableEncryption()
        {
            var pipeline = _sut.EnableEncryption("AdAstraAndBeyond")
                .Container.GetInstance<IPipeline>();
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = pipeline.OnSend(message).Body;

            Assert.Inconclusive();
        }

        [Test]
        public void ShouldNotIncludeQuotesWhenConvertingKeyToArray()
        {
            Assert.Inconclusive();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfSecretKeyIsNull()
        {
            _sut.EnableEncryption(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ShouldThrowExceptionIfSecretKeyIsTooShort()
        {
            _sut.EnableEncryption(new string('*', 15));
        }

        [Test]
        public void ShouldOverrideRegistration()
        {
            _sut.OverrideRegistration<ICorrelationStrategy, EmptyStrategy>();

            var actual = _sut.Container.GetInstance<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<EmptyStrategy>());
        }

        [Test]
        public void ShouldGetBus()
        {
            var actual = _sut.GetBus();

            Assert.That(actual, Is.Not.Null);

            actual.Dispose();
        }

        [Test]
        public void ShouldGetBusAndSetConfigurationFromConnectionString()
        {
            const string username = "admin";
            const string password = "password";

            var connectionString = String.Format("username={0};password={1}", username, password);

            var actual = _sut.GetBus(connectionString);

            Assert.That(actual, Is.Not.Null);

            var config = _sut.Container.GetInstance<IConnectionConfiguration>();

            Assert.That(config.Username, Is.EqualTo(username));
            Assert.That(config.Password, Is.EqualTo(password));

            actual.Dispose();
        }
    }
}