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

namespace StarMQ.Test.Consume
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Message;
    using System;

    public class ConsumerFactoryTest
    {
        private Mock<IConnection> _connection;
        private Mock<Func<IConsumer>> _getConsumer; 
        private IConsumerFactory _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _getConsumer = new Mock<Func<IConsumer>>();

            _sut = new ConsumerFactory(_getConsumer.Object, _connection.Object);

            var consumer = new BasicConsumer(new ConnectionConfiguration(), new Mock<IConnection>().Object,
                new Mock<IOutboundDispatcher>().Object, new Mock<IHandlerManager>().Object,
                new Mock<ILog>().Object, new Mock<INamingStrategy>().Object, new Mock<IPipeline>().Object,
                new Mock<ISerializationStrategy>().Object);
            _getConsumer.Setup(x => x()).Returns(consumer);
        }

        [Test]
        public void ShouldCreatePersistentConsumer()
        {
            var actual = _sut.CreateConsumer(false);

            Assert.That(actual, Is.TypeOf(typeof(PersistentConsumerDecorator)));
        }

        [Test]
        public void ShouldCreateBasicConsumer()
        {
            var actual = _sut.CreateConsumer(true);

            Assert.That(actual, Is.TypeOf<BasicConsumer>());
        }
    }
}