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
    using Exception;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using StarMQ.Core;
    using System;
    using System.Net.Sockets;
    using IConnection = RabbitMQ.Client.IConnection;

    public class PersistentConnectionTest
    {
        private Mock<IConnection> _connection;
        private Mock<ConnectionFactory> _factory;
        private Mock<ILog> _log;
        private PersistentConnection _sut;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _factory = new Mock<ConnectionFactory>();
            _log = new Mock<ILog>();

            _factory.Setup(x => x.CreateConnection()).Returns(_connection.Object);

            _sut = new PersistentConnection(new ConnectionConfiguration(), _factory.Object, _log.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void ShouldFireOnConnected()
        {
            var flag = false;

            _sut.OnConnected += () => flag = true;

            _connection.Raise(x => x.ConnectionShutdown += null,
                new ShutdownEventArgs(It.IsAny<ShutdownInitiator>(), It.IsAny<ushort>(), String.Empty));

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldRetryOnBrokerUnreachableException()
        {
            var config = new ConnectionConfiguration { Reconnect = 10 };
            var factory = new Mock<ConnectionFactory>();

            factory.SetupSequence(x => x.CreateConnection())
                .Throws(new BrokerUnreachableException(null, null, null))
                .Returns(_connection.Object);

            var sut = new PersistentConnection(config, factory.Object, _log.Object);

            factory.Verify(x => x.CreateConnection(), Times.Exactly(2));

            sut.Dispose();
        }

        [Test]
        public void ShouldRetryOnSocketException()
        {
            var config = new ConnectionConfiguration { Reconnect = 10 };
            var factory = new Mock<ConnectionFactory>();

            factory.SetupSequence(x => x.CreateConnection())
                .Throws(new SocketException())
                .Returns(_connection.Object);

            var sut = new PersistentConnection(config, factory.Object, _log.Object);

            factory.Verify(x => x.CreateConnection(), Times.Exactly(2));

            sut.Dispose();
        }

        [Test]
        public void ShouldFireOnDisconnectedWhenShutdown()
        {
            var flag = false;

            _sut.OnDisconnected += () => flag = true;

            _connection.Raise(x => x.ConnectionShutdown += null,
                new ShutdownEventArgs(It.IsAny<ShutdownInitiator>(), It.IsAny<ushort>(), String.Empty));

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldReconnectWhenDisconnected()
        {
            _connection.Raise(x => x.ConnectionShutdown += null,
                new ShutdownEventArgs(It.IsAny<ShutdownInitiator>(), It.IsAny<ushort>(), String.Empty));

            _factory.Verify(x => x.CreateConnection(), Times.Exactly(2));
        }

        [Test]
        public void ShouldNotReconnectIfDisconnectedAfterDispose()
        {
            _sut.Dispose();

            _connection.Raise(x => x.ConnectionShutdown += null,
                new ShutdownEventArgs(It.IsAny<ShutdownInitiator>(), It.IsAny<ushort>(), String.Empty));

            _factory.Verify(x => x.CreateConnection(), Times.Once);
        }

        [Test]
        public void ShouldCreateModel()
        {
            _connection.Setup(x => x.IsOpen).Returns(true);

            _sut.CreateModel();

            _connection.Verify(x => x.CreateModel(), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfNotConnected()
        {
            _sut.CreateModel();
        }

        [Test]
        public void ShouldDispose()
        {
            _connection.Setup(x => x.IsOpen).Returns(true);

            _sut.CreateModel();

            Assert.That(_sut.IsConnected, Is.True);

            _sut.Dispose();

            Assert.That(_sut.IsConnected, Is.False);
        }
    }
}