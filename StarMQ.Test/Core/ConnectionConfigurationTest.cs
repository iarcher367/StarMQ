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
    using NUnit.Framework;
    using StarMQ.Core;

    public class ConnectionConfigurationTest
    {
        private IConnectionConfiguration _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new ConnectionConfiguration();
        }

        [Test]
        public void ShouldSetDefaultHeartbeat()
        {
            Assert.That(_sut.Heartbeat, Is.EqualTo(10));
        }

        [Test]
        public void ShouldSetDefaultHost()
        {
            Assert.That(_sut.Host, Is.EqualTo("localhost"));
        }

        [Test]
        public void ShouldSetDefaultPassword()
        {
            Assert.That(_sut.Password, Is.EqualTo("guest"));
        }

        [Test]
        public void ShouldSetDefaultPort()
        {
            Assert.That(_sut.Port, Is.EqualTo(5672));
        }

        [Test]
        public void ShouldSetDefaultPrefetchCount()
        {
            Assert.That(_sut.PrefetchCount, Is.EqualTo(50));
        }

        [Test]
        public void ShouldSetDefaultReconnect()
        {
            Assert.That(_sut.Reconnect, Is.EqualTo(5000));
        }

        [Test]
        public void ShouldSetDefaultTimeout()
        {
            Assert.That(_sut.Timeout, Is.EqualTo(10000));
        }

        [Test]
        public void ShouldSetDefaultUsername()
        {
            Assert.That(_sut.Username, Is.EqualTo("guest"));
        }

        [Test]
        public void ShouldSetDefaultVirtualHost()
        {
            Assert.That(_sut.VirtualHost, Is.EqualTo("/"));
        }
    }
}