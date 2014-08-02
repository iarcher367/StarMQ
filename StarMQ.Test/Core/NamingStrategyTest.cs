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

namespace StarMQ.Test.Core
{
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;

    public class NamingStrategyTest
    {
        private const string SerializedName = "StarMQ.Master";

        private INamingStrategy _sut;
        private Mock<ITypeNameSerializer> _typeNameSerializer;

        [SetUp]
        public void Setup()
        {
            _typeNameSerializer = new Mock<ITypeNameSerializer>();

            _sut = new NamingStrategy(_typeNameSerializer.Object);

            _typeNameSerializer.Setup(x => x.Serialize(It.Is<Type>(y => y == typeof(string))))
                .Returns(SerializedName);
        }

        [Test]
        public void ShouldGetAlternateName()
        {
            var exchange = new Exchange().WithName(SerializedName);

            var actual = _sut.GetAlternateName(exchange);

            Assert.That(actual, Is.EqualTo(String.Format("AE:{0}", SerializedName)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetAlternateNameShouldThrowExceptionIfExchangeIsNull()
        {
            _sut.GetAlternateName(null);
        }

        [Test]
        public void ShouldGetConsumerTag()
        {
            Guid result;
            var actual = _sut.GetConsumerTag();

            Assert.That(Guid.TryParse(actual, out result), Is.True);
        }

        [Test]
        public void ShouldGetDeadLetterName()
        {
            var actual = _sut.GetDeadLetterName(SerializedName);

            Assert.That(actual, Is.EqualTo(String.Format("DLX:{0}", SerializedName)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetDeadLetterNameShouldThrowExceptionIfNameIsNull()
        {
            _sut.GetDeadLetterName(null);
        }

        [Test]
        public void ShouldGenerateExchangeName()
        {
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
            var actual = _sut.GetQueueName(typeof(string));

            Assert.That(actual, Is.EqualTo(SerializedName));

            _typeNameSerializer.Verify(x => x.Serialize(It.Is<Type>(y => y == typeof(string))),
                Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetQueueNameShouldThrowExceptionIfTypeIsNull()
        {
            _sut.GetQueueName(null);
        }
    }
}