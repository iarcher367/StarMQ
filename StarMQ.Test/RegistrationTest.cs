namespace StarMQ.Test
{
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;

    public class RegistrationTest
    {
        private const double CompressionRatio = 0.7;
        private const string Content = "Lorem ipsum dolor sit amet, est ea dicit verear albucius. " +
            "Populo phaedrum efficiantur has no, usu ex decore accusamus, ad nec etiam mazim " +
            "constituto. Liber prompta reprehendunt ea sea, no case fierent qui, elit viderer an " +
            "vix. Eum ea harum veritus. Dicunt labitur quaestio eu nam. Vide graece democritum.";

        internal class EmptyStrategy : ICorrelationStrategy
        {
            public string GenerateCorrelationId() { return String.Empty; }
        }

        [Test]
        public void ShouldReturnBasicPublisherIfConfirmsDisabled()
        {
            var container = Registration.RegisterServices();

            var actual = container.GetInstance<IPublisher>();

            Assert.That(actual, Is.TypeOf<BasicPublisher>());
        }

        [Test]
        public void ShouldReturnConfirmsPublisherIfConfirmsEnabled()
        {
            var container = Registration.RegisterServices();
            var configuration = container.GetInstance<IConnectionConfiguration>();

            Global.ParseConfiguration(configuration, "publisherconfirms=true");

            var actual = container.GetInstance<IPublisher>();

            Assert.That(actual, Is.TypeOf<ConfirmPublisher>());
        }

        [Test]
        public void ShouldAllowOverridingRegistrations()
        {
            var container = Registration.RegisterServices();
            container.RegisterSingle<ICorrelationStrategy, EmptyStrategy>();

            var actual = container.GetInstance<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<EmptyStrategy>());
        }

        [Test]
        public void ShouldEnableCompression()
        {
            var container = Registration.RegisterServices();

            Registration.EnableCompression(container);

            var pipeline = container.GetInstance<IPipeline>();
            var data = new JsonSerializer().ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = pipeline.OnSend(message).Body;

            Assert.That(body.Length, Is.LessThan(message.Body.Length * CompressionRatio));
        }
    }
}