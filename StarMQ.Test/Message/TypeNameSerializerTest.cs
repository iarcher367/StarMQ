namespace StarMQ.Test.Message
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;
    using System.Collections.Concurrent;

    public class TypeNameSerializerTest
    {
        private ITypeNameSerializer _sut;

        private readonly string _name = typeof(Properties).AssemblyQualifiedName;

        [SetUp]
        public void Setup()
        {
            _sut = new TypeNameSerializer();
        }

        [Test]
        public void ShouldDeserializeNameToType()
        {
            var actual = _sut.Deserialize(_name);

            Assert.That(actual, Is.EqualTo(typeof(Properties)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfNameIsNull()
        {
            _sut.Deserialize(null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfTypeIsUnknown()
        {
            const string input = "StarMQ.Swords.Excalibur:StarMQ";

            _sut.Deserialize(input);
        }

        [Test]
        public void ShouldSerializeTypeToString()
        {
            var actual = _sut.Serialize(typeof(Properties));

            Assert.That(actual, Is.EqualTo(_name));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfTypeIsNull()
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