namespace StarMQ.Test.Core
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Message;
    using System;

    public class NamingStrategyTest
    {
        private const string SerializedName = "StarMQ.Master";

        private INamingStrategy _sut;
        private Mock<ITypeNameSerializer> _typeNameSerializer;

        [SetUp]
        public void Setup()
        {
            _typeNameSerializer = new Mock<ITypeNameSerializer>(MockBehavior.Strict);
            _sut = new NamingStrategy(_typeNameSerializer.Object);
        }

        [Test]
        public void ShouldGenerateAlternateExchangeName()
        {
            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetAlternateExchangeName(typeof(string));

            Assert.That(actual, Is.EqualTo(String.Format("AE:{0}", SerializedName)));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAlternateExchangeNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetAlternateExchangeName(null);
        }

        [Test]
        public void ShouldGenerateAlternateQueueName()
        {
            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetAlternateQueueName(typeof(string));

            Assert.That(actual, Is.EqualTo(String.Format("AE:{0}", SerializedName)));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAlternateQueueNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetAlternateQueueName(null);
        }

        [Test]
        public void ShouldGetConsumerTag()
        {
            Guid result;
            var actual = _sut.GetConsumerTag();

            Assert.That(Guid.TryParse(actual, out result), Is.True);
        }

        [Test]
        public void ShouldGenerateDeadLetterExchangeName()
        {
            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetDeadLetterExchangeName(typeof(string));

            Assert.That(actual, Is.EqualTo(String.Format("DLX:{0}", SerializedName)));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDeadLetterExchangeNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetDeadLetterExchangeName(null);
        }

        [Test]
        public void ShouldGenerateDeadLetterQueueName()
        {
            const string id = "InstanceA";

            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetDeadLetterQueueName(typeof(string), id);

            Assert.That(actual, Is.EqualTo(String.Format("DLX:{0}:{1}", SerializedName, id)));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDeadLetterQueueNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetDeadLetterQueueName(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDeadLetterQueueNameShouldThrowExceptionIfSubscriberIdIsNull()
        {
            _sut.GetDeadLetterQueueName(typeof(string), null);
        }

        [Test]
        public void ShouldGenerateExchangeName()
        {
            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetExchangeName(typeof(string));

            Assert.That(actual, Is.EqualTo(SerializedName));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetExchangeNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetExchangeName(null);
        }

        [Test]
        public void ShouldGenerateQueueName()
        {
            const string id = "InstanceA";

            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);

            var actual = _sut.GetQueueName(typeof(string), id);

            Assert.That(actual, Is.EqualTo(String.Format("{0}:{1}", SerializedName, id)));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetQueueNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetQueueName(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetQueueNameShouldThrowExceptionIfSubscriberIdIsNull()
        {
            _sut.GetQueueName(typeof(string), null);
        }
    }
}