﻿namespace StarMQ.Test.Core
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Core;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class OutboundDispatcherTest
    {
        private const int Delay = 10;

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private IOutboundDispatcher _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);

            _sut = new OutboundDispatcher(_configuration.Object, _connection.Object, _log.Object);
        }

        [Test]
        public void ShouldOpenChannel()
        {
            _connection.Verify(x => x.CreateModel(), Times.Once);
        }

        [Test]
        public async Task ShouldOpenChannelAndUnblockWhenOnConnectFires()
        {
            var count = 0;

            _connection.Raise(x => x.OnDisconnected += null);

            await _sut.Invoke(x => { count += 5; });

            await Task.Delay(Delay);

            Assert.That(count, Is.EqualTo(0));

            _connection.Raise(x => x.OnConnected += null);

            await Task.Delay(Delay);

            Assert.That(count, Is.EqualTo(5));

            _connection.Verify(x => x.CreateModel(), Times.Exactly(2));
        }

        [Test]
        public async Task ShouldBlockWhenOnDisconnectFires()
        {
            var count = 0;

            await _sut.Invoke(x => { count += 3; });

            await Task.Delay(Delay);

            _connection.Raise(x => x.OnDisconnected += null);

            await _sut.Invoke(x => { count += 5; });

            await Task.Delay(Delay);

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public async Task ShouldDispatchAction()
        {
            var flag = false;

            await _sut.Invoke(() => { flag = true; });

            await Task.Delay(Delay);

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Invoke((Action)null);
        }

        [Test]
        public async Task ShouldDispatchActionUsingInternalModel()
        {
            var flag = false;

            await _sut.Invoke(x => { flag = true; });

            await Task.Delay(Delay);

            Assert.That(flag, Is.True);
        }

        [Test]
        public async Task ShouldRetryUntilTimeoutIfChannelFails()
        {
            _configuration.Setup(x => x.Timeout).Returns(125);

            var count = 0;

            await _sut.Invoke(x =>
                {
                    count += 2;
                    throw new NotSupportedException();
                });

            await Task.Delay(140);

            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionUsingInternalModelIsNull()
        {
            _sut.Invoke((Action<IModel>)null);
        }

        [Test]
        public void ShouldDispose()
        {
            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ShouldNotDisposeMultipleTimes()
        {
            _sut.Dispose();
            _sut.Dispose();
            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}