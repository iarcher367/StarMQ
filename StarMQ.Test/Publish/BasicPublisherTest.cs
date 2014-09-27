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

namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Publish;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BasicPublisherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private BasePublisher _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();

            _connection.Setup(x => x.CreateModel()).Returns(new Mock<IModel>().Object);

            _sut = new BasicPublisher(_connection.Object, _log.Object);
        }

        [Test]
        public async Task ShouldInvokeAction()
        {
            var flag = false;

            await _sut.Publish(x => flag = true);

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Publish(null);
        }
    }
}