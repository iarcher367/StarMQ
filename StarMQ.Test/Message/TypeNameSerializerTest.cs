namespace StarMQ.Test.Message
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Message;
    using System;
    using System.Collections.Concurrent;

    public class TypeNameSerializerTest
    {
        private ITypeNameSerializer _sut;

        private const string Name = "StarMQ.Message.Properties:StarMQ";

        [SetUp]
        public void Setup()
        {
            _sut = new TypeNameSerializer();
        }

        [Test]
        public void ShouldDeserializeNameToType()
        {
            var actual = _sut.Deserialize(Name);

            Assert.That(actual, Is.EqualTo(typeof(Properties)));
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionForTooFewDelimitersInName()
        {
            _sut.Deserialize(Name.Split(':')[0]);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionForTooManyDelimitersInName()
        {
            const string input = Name + ":bug";

            _sut.Deserialize(input);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionForUnknownType()
        {
            const string input = "StarMQ.Swords.Excalibur:StarMQ";

            _sut.Deserialize(input);
        }

        [Test]
        public void ShouldSerializeTypeToString()
        {
            var actual = _sut.Serialize(typeof(Properties));

            Assert.That(actual, Is.EqualTo(Name));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionForNullTypeParameter()
        {
            _sut.Serialize(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfSerializedNameIsTooLong()
        {
            _sut.Serialize(typeof(BlockingCollection<ConcurrentDictionary<int, CorrelationStrategy>>));
        }
    }
}