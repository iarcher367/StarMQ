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
    using System.Collections.Generic;

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
            var item = new KeyValuePair<string, object>("signature", "er2eECZ3is4ag6od");
            var actual = _sut.WithHeader(item).Properties.Headers;

            Assert.That(actual.ContainsKey(item.Key), Is.True);
            Assert.That(actual[item.Key], Is.EqualTo(item.Value));
        }
    }
}