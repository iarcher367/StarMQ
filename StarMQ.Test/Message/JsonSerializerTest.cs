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

namespace StarMQ.Test.Message
{
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

    public class JsonSerializerTest
    {
        private const string Content = "Hello world!";

        private readonly byte[] _data = { 34, 72, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100, 33, 34 };

        private ISerializer _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new JsonSerializer();
        }

        [Test]
        public void ShouldUseTypeNameHandlingAuto()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void ShouldSerializeObjectToByteArray()
        {
            var actual = _sut.ToBytes(Content);

            Assert.That(actual, Is.EquivalentTo(_data));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToBytesThrowExceptionIfContentIsNull()
        {
            _sut.ToBytes<string>(null);
        }

        [Test]
        public void ShouldDeserializeByteArrayToObject()
        {
            var actual = _sut.ToObject(_data, typeof(string));

            Assert.That(actual, Is.EqualTo(Content));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToObjectThrowExceptionIfContentIsNull()
        {
            _sut.ToObject(null, typeof(string));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldToObjectThrowExceptionIfContentIsEmpty()
        {
            _sut.ToObject(new byte[0], typeof(string));
        }
    }
}
