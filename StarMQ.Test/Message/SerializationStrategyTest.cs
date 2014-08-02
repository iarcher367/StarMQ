namespace StarMQ.Test.Message
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

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
            _correlationStrategy = new Mock<ICorrelationStrategy>();
            _serializer = new Mock<ISerializer>();
            _typeNameSerializer = new Mock<ITypeNameSerializer>();
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

            _serializer.VerifyAll();
        }

        [Test]
        public void ShouldSerializeMessageToByteArrayMessage()
        {
            var serializedBody = new byte[5];

            _serializer.Setup(x => x.ToBytes(It.IsAny<string>())).Returns(serializedBody);

            var properties = _message.Properties;
            var actual = _sut.Serialize(_message);

            Assert.That(actual.Body, Is.SameAs(serializedBody));
            Assert.That(actual.Properties, Is.SameAs(properties));
        }

        [Test]
        public void ShouldSetTypeProperty()
        {
            const string typeName = "System.String";

            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(typeName);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.Type, Is.EqualTo(typeName));

            _typeNameSerializer.VerifyAll();
        }

        [Test]
        public void ShouldSetCorrelationIdIfNotPopulated()
        {
            var guid = Guid.NewGuid().ToString();

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(guid);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));

            _correlationStrategy.VerifyAll();
        }

        [Test]
        public void ShouldNotOverwriteCorrelationId()
        {
            var guid = Guid.NewGuid().ToString();

            _message.Properties.CorrelationId = guid;

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(Guid.NewGuid().ToString);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));

            _correlationStrategy.Verify(x => x.GenerateCorrelationId(), Times.Never());
        }
    }
}