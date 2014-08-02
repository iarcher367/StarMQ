namespace StarMQ.Test.Core
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Message;
    using System;

    public class NamingStrategyTest
    {
        private const string Name = "StarMQ.Master";

        private INamingStrategy _sut;
        private Mock<ITypeNameSerializer> _typeNameSerializer;

        [SetUp]
        public void Setup()
        {
            _typeNameSerializer = new Mock<ITypeNameSerializer>();
            _sut = new NamingStrategy(_typeNameSerializer.Object);
        }

        [Test]
        public void ShouldGetExchangeName()
        {
            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(Name);

            var actual = _sut.GetExchangeName(typeof(string));

            Assert.That(actual, Is.EqualTo(Name));
        }

        [Test]
        public void ShouldGetQueueName()
        {
            const string id = "InstanceA";

            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(Name);

            var actual = _sut.GetQueueName(typeof(string), id);

            Assert.That(actual, Is.EqualTo(String.Format("{0}:{1}", Name, id)));
        }
    }
}