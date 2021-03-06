﻿#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Test.Message
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;

    public class SerializationStrategyTest
    {
        private const string Content = "Hello World!";

        private Mock<ICorrelationStrategy> _correlationStrategy;
        private Mock<ISerializer> _serializer;
        private Mock<ITypeNameSerializer> _typeNameSerializer;
        private ISerializationStrategy _sut;

        private IMessage<string> _message;

        [SetUp]
        public void Setup()
        {
            _correlationStrategy = new Mock<ICorrelationStrategy>();
            _serializer = new Mock<ISerializer>();
            _typeNameSerializer = new Mock<ITypeNameSerializer>();

            _sut = new SerializationStrategy(_correlationStrategy.Object, _serializer.Object,
                _typeNameSerializer.Object);

            _message = new Message<string>(Content);
        }

        [Test]
        public void ShouldDeserializeByteArrayMessageToMessage()
        {
            var body = new Properties { MessageId = "42" };
            var data = Helper.ToBytes(body);

            _serializer.Setup(x => x.ToObject(data, typeof(Properties))).Returns(body);
            _typeNameSerializer.Setup(x => x.Deserialize(It.IsAny<string>())).Returns(typeof(Properties));

            var properties = new Properties { Type = typeof(Properties).AssemblyQualifiedName };
            var message = new Message<byte[]>(data) { Properties = properties };

            var actual = _sut.Deserialize(message, typeof(Properties));

            Assert.That(actual.Body, Is.EqualTo(body));
            Assert.That(actual.Body, Is.InstanceOf<Properties>());
            Assert.That(actual.Properties, Is.SameAs(properties));

            _serializer.Verify(x => x.ToObject(data, typeof(Properties)), Times.Once);
            _typeNameSerializer.Verify(x => x.Deserialize(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void ShouldDeserializeByteArrayMessageToDefaultMessageIfNoType()
        {
            var body = new Properties { MessageId = "42" };
            var data = Helper.ToBytes(body);

            _serializer.Setup(x => x.ToObject(data, typeof(Properties))).Returns(body);

            var properties = new Properties();
            var message = new Message<byte[]>(data) { Properties = properties };

            var actual = _sut.Deserialize(message, typeof(Properties));

            Assert.That(actual.Body, Is.EqualTo(body));
            Assert.That(actual.Body, Is.InstanceOf<Properties>());
            Assert.That(actual.Properties, Is.SameAs(properties));

            _serializer.Verify(x => x.ToObject(data, typeof(Properties)), Times.Once);
            _typeNameSerializer.Verify(x => x.Deserialize(It.IsAny<string>()), Times.Never);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeShouldThrowExceptionIfMessageIsNull()
        {
            _sut.Deserialize(null, typeof(Properties));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeserializeShouldThrowExceptionIfDefaultTypeIsNull()
        {
            _sut.Deserialize(new Message<byte[]>(new byte[0]), null);
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

            _serializer.Verify(x => x.ToBytes(_message.Body), Times.Once);
        }

        [Test]
        public void ShouldSetTypeProperty()
        {
            const string typeName = "System.String";

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(String.Empty);
            _typeNameSerializer.Setup(x => x.Serialize(typeof(string))).Returns(typeName);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.Type, Is.EqualTo(typeName));
        }

        [Test]
        public void ShouldSetCorrelationIdIfNotPopulated()
        {
            var guid = Guid.NewGuid().ToString();

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(guid);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(String.Empty);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));
        }

        [Test]
        public void ShouldNotOverwriteCorrelationId()
        {
            var guid = Guid.NewGuid().ToString();

            _message.Properties.CorrelationId = guid;

            _correlationStrategy.Setup(x => x.GenerateCorrelationId()).Returns(Guid.NewGuid().ToString);
            _typeNameSerializer.Setup(x => x.Serialize(It.IsAny<Type>())).Returns(String.Empty);

            var actual = _sut.Serialize(_message);

            Assert.That(actual.Properties.CorrelationId, Is.EqualTo(guid));

            _correlationStrategy.Verify(x => x.GenerateCorrelationId(), Times.Never);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SerializeShouldThrowExceptionIfMessageIsNull()
        {
            _sut.Serialize<string>(null);
        }
    }
}