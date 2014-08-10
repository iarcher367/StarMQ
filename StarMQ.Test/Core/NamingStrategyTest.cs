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
    }
}