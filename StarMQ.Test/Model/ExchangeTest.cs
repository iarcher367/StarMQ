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

    public class ExchangeTest
    {
        private const string Expected = "StarMQ.Message:StarMQ";

        [Test]
        public void ShouldDefaultDurableToTrue()
        {
            var exchange = new Exchange();

            Assert.That(exchange.Durable, Is.True);
        }

        [Test]
        public void ShouldSetExchangeName()
        {
            var exchange = new Exchange().WithName(Expected);

            Assert.That(exchange.Name, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfNameIsNull()
        {
            new Exchange().WithName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfNameIsTooLong()
        {
            new Exchange().WithName(new String('*', 256));
        }

        [Test]
        public void ShouldSetAutoDelete()
        {
            var actual = new Exchange().WithAutoDelete(true).AutoDelete;

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldSetDurable()
        {
            var actual = new Exchange().WithDurable(false).Durable;

            Assert.That(actual, Is.False);
        }

        [Test]
        public void ShouldSetType()
        {
            var actual = new Exchange().WithType(ExchangeType.Headers).Type;

            Assert.That(actual, Is.EqualTo(ExchangeType.Headers));
        }

        [Test]
        public void ShouldSetAlternateExchangeName()
        {
            var actual = new Exchange().WithAlternateExchangeName(Expected).AlternateExchangeName;

            Assert.That(actual, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfAlternateExchangeNameIsNull()
        {
            new Exchange().WithAlternateExchangeName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfAlternateExchangeNameIsTooLong()
        {
            new Exchange().WithAlternateExchangeName(new String('*', 256));
        }

    }
}