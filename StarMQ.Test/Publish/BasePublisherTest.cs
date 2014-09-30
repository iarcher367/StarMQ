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
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Message;
    using StarMQ.Publish;
    using IConnection = StarMQ.Core.IConnection;

    public class BasePublisherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<IPipeline> _pipeline;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private BasePublisher _sut;

        private Mock<IBasicProperties> _properties;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _pipeline = new Mock<IPipeline>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);

            _sut = new BasicPublisher(_connection.Object, _log.Object, _pipeline.Object,
                _serializationStrategy.Object);

            _properties = new Mock<IBasicProperties>();
        }

        [Test]
        public void ShouldBindBasicReturn()
        {
            var count = 0;

            _sut.BasicReturn += (o, e) => { count += 2; };

            _model.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(count, Is.EqualTo(2));

            _connection.Verify(x => x.CreateModel(), Times.Once);
        }

        [Test]
        public void ShouldUnbindBasicReturnIfOnDisconnectedFires()
        {
            var flag = false;

            _sut.BasicReturn += (o, e) => { flag = true; };

            _connection.Raise(x => x.OnDisconnected += null);

            _model.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(flag, Is.False);
        }

        [Test]
        public void ShouldFireBasicReturnEventIfModelBasicReturnFires()
        {
            var flag = false;

            _sut.BasicReturn += (o, e) => { flag = true; };

            _model.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldDispose()
        {
            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}