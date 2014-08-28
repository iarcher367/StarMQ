namespace StarMQ.Test.Message
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Message;
    using System;
    using StarMQ.Model;

    public class SerializationStrategyTest
    {
        private const string Content = "Hello World!";

        private Mock<ICorrelationStrategy> _correlationStrategy;
        private Mock<ISerializer> _serializer;
        private ISerializationStrategy _sut;
        private Mock<ITypeNameSerializer> _typeNameSerializer;

        private IMessage<string> _message;

        [SetUp]
        public void Setup()
        {
            _correlationStrategy = new Mock<ICorrelationStrategy>(MockBehavior.Strict);
            _serializer = new Mock<ISerializer>(MockBehavior.Strict);
            _typeNameSerializer = new Mock<ITypeNameSerializer>(MockBehavior.Strict);

            _sut = new SerializationStrategy(_correlationStrategy.Object, _serializer.Object, _typeNameSerializer.Object);

            _message = new Message<string>(Content);
        }

        [Test]
        public void ShouldDeserializeByteArrayMessageToMessage()
        {
            var data = new JsonSerializer().ToBytes(Content);

            _serializer.Setup(x => x.ToObject<string>(data)).Returns(Content);

            var properties = new Properties();
            var message = new Message<byte[]>(data) { Properties = properties };

            var actual = _sut.Deserialize<string>(message);

            Assert.That(actual.Body, Is.EqualTo(Content));
            Assert.That(actual.Properties, Is.SameAs(properties));

            _serializer.Verify(x => x.ToObject<string>(data), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeShouldThrowExceptionIfMessageIsNull()
        {
            _sut.Deserialize<string>(null);
        }

        [Test]
        public void ShouldSerializeMessageToByteArrayMessage()
        {
            var serializedBody = new byte[5];

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(String.Empty);
            _serializer.Setup(x => x.ToBytes(It.IsAny<string>())).Returns(serializedBody);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(String.Empty);

            var properties = _message.Properties;
            var actual = _sut.Serialize(_message);

            Assert.That(actual.Body, Is.SameAs(serializedBody));
            Assert.That(actual.Properties, Is.SameAs(properties));

            _correlationStrategy.VerifyAll();
            _serializer.Verify(x => x.ToBytes(_message.Body), Times.Once);
            _typeNameSerializer.VerifyAll();
        }

        [Test]
        public void ShouldSetTypeProperty()
        {
            const string typeName = "System.String";

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(String.Empty);
            _serializer.Setup(x => x.ToBytes(It.IsAny<string>())).Returns(new byte[0]);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(typeName);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.Type, Is.EqualTo(typeName));

            _correlationStrategy.VerifyAll();
            _serializer.VerifyAll();
            _typeNameSerializer.VerifyAll();
        }

        [Test]
        public void ShouldSetCorrelationIdIfNotPopulated()
        {
            var guid = Guid.NewGuid().ToString();

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(guid);
            _serializer.Setup(x => x.ToBytes(It.IsAny<string>())).Returns(new byte[0]);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(String.Empty);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));

            _correlationStrategy.Verify(x => x.GenerateCorrelationId(), Times.Once);
            _serializer.VerifyAll();
            _typeNameSerializer.VerifyAll();
        }

        [Test]
        public void ShouldNotOverwriteCorrelationId()
        {
            var guid = Guid.NewGuid().ToString();

            _message.Properties.CorrelationId = guid;

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(Guid.NewGuid().ToString);
            _serializer.Setup(x => x.ToBytes(It.IsAny<string>())).Returns(new byte[0]);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(String.Empty);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));

            _correlationStrategy.Verify(x => x.GenerateCorrelationId(), Times.Never());
            _serializer.VerifyAll();
            _typeNameSerializer.VerifyAll();
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void SerializeShouldThrowExceptionIfMessageIsNull()
        {
            _sut.Serialize<string>(null);
        }
    }
}