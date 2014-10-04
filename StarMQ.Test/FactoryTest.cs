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
            var factory = new Factory().Container.Resolve<ConnectionFactory>();

            Assert.That(factory.Port, Is.Not.EqualTo(-1));
            Assert.That(factory.RequestedHeartbeat, Is.Not.EqualTo(0));
        }

        [Test]
        public void ShouldSetClientPropertiesInConnectionFactory()
        {
            var factory = new Factory().Container.Resolve<ConnectionFactory>();

            Assert.That(factory.ClientProperties.Count, Is.GreaterThan(5));
        }

        [Test]
        public void ShouldReturnBasicPublisherIfConfirmsDisabled()
        {
            var actual = _sut.Container.Resolve<IPublisher>();

            Assert.That(actual, Is.TypeOf<BasicPublisher>());
        }

        [Test]
        public void ShouldReturnConfirmsPublisherIfConfirmsEnabled()
        {
            var configuration = _sut.Container.Resolve<IConnectionConfiguration>();

            Global.ParseConfiguration(configuration, "publisherconfirms=true");

            var actual = _sut.Container.Resolve<IPublisher>();

            Assert.That(actual, Is.TypeOf<ConfirmPublisherDecorator>());
        }

        [Test]
        public void ShouldAddInterceptor()
        {
            _sut.AddInterceptor(new CompressionInterceptor());

            var data = Helper.ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = _sut.Container.Resolve<IPipeline>().OnSend(message).Body;

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
                .Container.Resolve<IPipeline>();
            var data = Helper.ToBytes(Content);
            var message = new Message<byte[]>(data);

            var body = pipeline.OnSend(message).Body;

            Assert.That(body.Length, Is.LessThan(message.Body.Length * CompressionRatio));
        }

        [Test]
        public void ShouldEnableEncryption()
        {
            var pipeline = _sut.EnableEncryption("AdAstraAndBeyond")
                .Container.Resolve<IPipeline>();
            var data = Helper.ToBytes(Content);
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
        [ExpectedException(typeof(InvalidValueException))]
        public void ShouldThrowExceptionIfSecretKeyIsTooShort()
        {
            _sut.EnableEncryption(new string('*', 15));
        }

        [Test]
        [ExpectedException(typeof(InvalidValueException))]
        public void ShouldThrowExceptionIfSecretKeyIsTooLong()
        {
            _sut.EnableEncryption(new string('*', 33));
        }

        [Test]
        public void ShouldOverrideRegistration()
        {
            _sut.OverrideRegistration<ICorrelationStrategy, EmptyStrategy>();

            var actual = _sut.Container.Resolve<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<EmptyStrategy>());
        }

        [Test]
        public void ShouldOverrideRegistrationWithContext()
        {
            _sut.OverrideRegistration<ICorrelationStrategy>(x => new EmptyStrategy());

            var actual = _sut.Container.Resolve<ICorrelationStrategy>();

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

            var config = _sut.Container.Resolve<IConnectionConfiguration>();

            Assert.That(config.Username, Is.EqualTo(username));
            Assert.That(config.Password, Is.EqualTo(password));

            actual.Dispose();
        }
    }
}