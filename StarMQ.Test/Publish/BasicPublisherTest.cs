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
    using StarMQ.Message;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class BasicPublisherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<IPipeline> _pipeline;
        private Mock<IBasicProperties> _properties;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private BasePublisher _sut;

        private IMessage<string> _message;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _pipeline = new Mock<IPipeline>();
            _properties = new Mock<IBasicProperties>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _model.Setup(x => x.CreateBasicProperties()).Returns(_properties.Object);

            _sut = new BasicPublisher(_connection.Object, _log.Object, _pipeline.Object,
                _serializationStrategy.Object);

            _message = new Message<string>(String.Empty);
        }

        [Test]
        public async Task ShouldSetProperties()
        {
            const int priority = 9;
            var publishAction = new Mock<Action<IModel, IBasicProperties, byte[]>>();

            _pipeline.Setup(x => x.OnSend(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0])
                {
                    Properties = new Properties { Priority = priority }
                });

            await _sut.Publish(_message, publishAction.Object);

            _properties.VerifySet(x => x.Priority = priority, Times.Once);
            publishAction.Verify(x => x(_model.Object, _properties.Object, It.IsAny<byte[]>()));
        }

        [Test]
        public async Task ShouldInvokeActionAndProcessMessage()
        {
            var publishAction = new Mock<Action<IModel, IBasicProperties, byte[]>>();
            var serialized = new Mock<IMessage<byte[]>>();

            _pipeline.Setup(x => x.OnSend(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0]) { Properties = new Properties() });
            _serializationStrategy.Setup(x => x.Serialize(_message)).Returns(serialized.Object);

            await _sut.Publish(_message, publishAction.Object);

            _serializationStrategy.Verify(x => x.Serialize(_message), Times.Once);
            _pipeline.Verify(x => x.OnSend(serialized.Object), Times.Once);
            publishAction.Verify(x => x(_model.Object, _properties.Object, It.IsAny<byte[]>()));
        }

        [Test]
        [ExpectedException(typeof(IOException))]
        public async Task ShouldHaveDispatcherRetryOnIoException()
        {
            _model.Setup(x => x.BasicAck(0, false))
                .Throws(new IOException());
            _pipeline.Setup(x => x.OnSend(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0]) { Properties = new Properties() });

            await _sut.Publish(_message, (x, y, z) => x.BasicAck(0, false));

            _model.Verify(x => x.BasicAck(0, false), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task ShouldHaveDispatcherRetryOnNotSupportedException()
        {
            _model.Setup(x => x.BasicAck(0, false))
                .Throws(new NotSupportedException());
            _pipeline.Setup(x => x.OnSend(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0]) { Properties = new Properties() });

            await _sut.Publish(_message, (x, y, z) => x.BasicAck(0, false));

            _model.Verify(x => x.BasicAck(0, false), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldThrowExceptionIfActionIsNull()
        {
            await _sut.Publish(_message, null);
        }
    }
}