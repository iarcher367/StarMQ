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

namespace StarMQ.Test.Model
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Model;
    using System;

    public class DeliveryContextTest
    {
        private DeliveryContext _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new DeliveryContext();
        }

        [Test]
        public void ShouldSetRoutingKey()
        {
            const string key = "alpha.omega";
            var actual = _sut.WithRoutingKey(key).RoutingKey;

            Assert.That(actual, Is.EqualTo(key));
        }

        [Test]
        public void ShouldSetHeader()
        {
            const string key = "signature";
            const string value = "er2eECZ3is4ag6od";
            var actual = _sut.WithHeader(key, value).Properties.Headers;

            Assert.That(actual.ContainsKey(key), Is.True);
            Assert.That(actual[key], Is.EqualTo(value));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfKeyIsNull()
        {
            _sut.WithHeader(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof (MaxLengthException))]
        public void ShouldThrowExceptionIfKeyIsTooLong()
        {
            _sut.WithHeader(new string('*', 256), String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfValueIsNull()
        {
            _sut.WithHeader(String.Empty, null);
        }
    }
}