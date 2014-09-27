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
    using NUnit.Framework;
    using StarMQ.Model;
    using System;

    public class MessageTest
    {
        [Test]
        public void ShouldSetBody()
        {
            const string body = "Lorem ipsum dolor sit amet, qui no timeam similique.";

            var sut = new Message<string>(body);

            Assert.That(sut.Body, Is.SameAs(body));
        }

        [Test]
        public void ShouldSetDefaultProperties()
        {
            var sut = new Message<string>(String.Empty);

            Assert.That(sut.Properties, Is.Not.Null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfBodyIsNull()
        {
            var sut = new Message<object>(null);
        }

        [Test]
        public void ShouldSetProperties()
        {
            var expected = new Properties();
            var sut = new Message<string>(String.Empty) { Properties = expected };

            Assert.That(sut.Properties, Is.SameAs(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfPropertiesIsNull()
        {
            var sut = new Message<string>(String.Empty) { Properties = null };
        }
    }
}
