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
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using StarMQ.Consume;
    using StarMQ.Core;
    using StarMQ.Message;
    using StarMQ.Model;
    using System;
    using System.IO;
    using System.Threading;
    using IConnection = StarMQ.Core.IConnection;

    public class BaseConsumerTest
    {
        private const string ConsumerTag = "a3467096-7250-47b8-b5d7-08472505fc2d";
        private const int Timeout = 1000;
        private const ulong DeliveryTag = 42;

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<IOutboundDispatcher> _dispatcher;
        private Mock<IHandlerManager> _handlerManager;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<INamingStrategy> _namingStrategy;
        private Mock<IPipeline> _pipeline;
        private Mock<IBasicProperties> _properties;
        private Mock<ISerializationStrategy> _serializationStrategy;
        private BaseConsumer _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _dispatcher = new Mock<IOutboundDispatcher>();
            _handlerManager = new Mock<IHandlerManager>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _namingStrategy = new Mock<INamingStrategy>();
            _pipeline = new Mock<IPipeline>();
            _properties = new Mock<IBasicProperties>();
            _serializationStrategy = new Mock<ISerializationStrategy>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);
            _namingStrategy.Setup(x => x.GetConsumerTag()).Returns(ConsumerTag);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        private void GenericSetup()
        {
            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                _handlerManager.Object, _log.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);
        }

        [Test]
        public void ShouldDiscardMessagesIfOnDisconnectedFires()
        {
            GenericSetup();

            _connection.Raise(x => x.OnDisconnected += null);

            Assert.Inconclusive();
        }

        [Test]
        public void CancelShouldFireConsumerCancelledEvent()
        {
            var flag = false;

            GenericSetup();

            _sut.ConsumerCancelled += (o, e) => flag = true;

            _sut.HandleBasicCancel(ConsumerTag);

            Assert.That(flag, Is.True);
        }

        [Test]
        public void CancelOkShouldDoNothing()
        {
            GenericSetup();

            _sut.HandleBasicCancelOk(ConsumerTag);
        }

        [Test]
        public void ConsumeOkShouldSetConsumerTag()
        {
            var tag = Guid.NewGuid().ToString();

            GenericSetup();

            _sut.HandleBasicConsumeOk(tag);

            Assert.That(_sut.ConsumerTag, Is.EqualTo(tag));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConsumeOkShouldThrowExceptionIfConsumerTagIsNull()
        {
            GenericSetup();

            _sut.HandleBasicConsumeOk(null);
        }

        private void DeliverSetup(Action<IHandlerRegistrar> configure)
        {
            var type = typeof(string);

            var handlerManager = new HandlerManager(_log.Object);
            configure(handlerManager);

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                handlerManager, _log.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);

            _pipeline.Setup(x => x.OnReceive(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0])
                {
                    Properties = new Properties { Type = type.FullName }
                });
            _serializationStrategy.Setup(x => x.Deserialize(It.IsAny<IMessage<byte[]>>(), typeof(Factory)))
                .Returns(new Message<dynamic>(new Factory())
                {
                    Properties = new Properties { Type = type.FullName }
                });
        }

        [Test]
        public void DeliverShouldProcessMessage()
        {
            var signal = new ManualResetEventSlim(false);

            DeliverSetup(x => x.Add<Factory>(y => signal.Set()));

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();

            _pipeline.Verify(x => x.OnReceive(It.IsAny<IMessage<byte[]>>()), Times.Once);
            _serializationStrategy.Verify(x => x.Deserialize(It.IsAny<IMessage<byte[]>>(), typeof(Factory)),
                Times.Once);
        }

        [Test]
        public void DeliverShouldNotBeginProcessingIfCancelled()
        {
            var count = 0;

            DeliverSetup(x => x.Add<Factory>(y => count++));

            _sut.Dispose();

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            Assert.That(count, Is.EqualTo(0));

            _pipeline.Verify(x => x.OnReceive(It.IsAny<IMessage<byte[]>>()), Times.Never);
            _serializationStrategy.Verify(x => x.Deserialize(It.IsAny<IMessage<byte[]>>(), typeof(Factory)),
                Times.Never);
        }

        [Test]
        public void DeliverShouldBasicAckWithDeliveryTag()
        {
            var signal = new ManualResetEventSlim(false);

            DeliverSetup(x => x.Add<Factory>(y => { }));

            _model.Setup(x => x.BasicAck(DeliveryTag, false))
                .Callback(signal.Set);

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();

            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
        }

        [Test]
        public void DeliverShouldBasicNackWithDeliveryTag()
        {
            var signal = new ManualResetEventSlim(false);

            _model.Setup(x => x.BasicNack(DeliveryTag, false, false))
                .Callback(signal.Set);

            DeliverSetup(x => x.Add<Factory>(y => new NackResponse()));

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();

            _model.Verify(x => x.BasicNack(DeliveryTag, false, false), Times.Once);
        }

        [Test]
        public void DeliverShouldBasicCancelOnUnsubscribeAction()
        {
            var signal = new ManualResetEventSlim(false);

            _model.Setup(x => x.BasicCancel(ConsumerTag))
                .Callback(signal.Set);

            DeliverSetup(x => x.Add<Factory>(y => new AckResponse { Action = ResponseAction.Unsubscribe }));

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();

            _model.Verify(x => x.BasicAck(DeliveryTag, false), Times.Once);
            _model.Verify(x => x.BasicCancel(ConsumerTag), Times.Once);
            _model.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void DeliverShouldBasicNackOnDeserializeException()
        {
            var signal = new ManualResetEventSlim(false);

            var type = typeof(string);

            var handlerManager = new HandlerManager(_log.Object);
            handlerManager.Add<Factory>(y => { });

            _sut = new BasicConsumer(_configuration.Object, _connection.Object, _dispatcher.Object,
                handlerManager, _log.Object, _namingStrategy.Object, _pipeline.Object,
                _serializationStrategy.Object);

            _pipeline.Setup(x => x.OnReceive(It.IsAny<IMessage<byte[]>>()))
                .Returns(new Message<byte[]>(new byte[0])
                {
                    Properties = new Properties { Type = type.FullName }
                });
            _serializationStrategy.Setup(x => x.Deserialize(It.IsAny<IMessage<byte[]>>(), typeof(Factory)))
                .Throws(new JsonReaderException());

            _model.Setup(x => x.BasicNack(DeliveryTag, false, false))
                .Callback(signal.Set);

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();
        }

        [Test]
        public void DeliverShouldDoNothingOnModelAlreadyClosedException()
        {
            var signal = new ManualResetEventSlim(false);

            DeliverSetup(x => x.Add<Factory>(y => { }));

            _model.Setup(x => x.BasicAck(DeliveryTag, false))
                .Callback(() =>
                {
                    signal.Set();
                    throw new AlreadyClosedException(null);
                });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();
        }

        [Test]
        public void DeliverShouldDoNothingOnModelIoException()
        {
            var signal = new ManualResetEventSlim(false);

            DeliverSetup(x => x.Add<Factory>(y => { }));

            _model.Setup(x => x.BasicAck(DeliveryTag, false))
                .Callback(() =>
                {
                    signal.Set();
                    throw new IOException();
                });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();
        }

        [Test]
        public void DeliverShouldDoNothingOnModelNotSupportedException()
        {
            var signal = new ManualResetEventSlim(false);

            DeliverSetup(x => x.Add<Factory>(y => { }));

            _model.Setup(x => x.BasicAck(DeliveryTag, false))
                .Callback(() =>
                {
                    signal.Set();
                    throw new NotSupportedException();
                });

            _sut.HandleBasicDeliver(ConsumerTag, DeliveryTag, false, String.Empty, String.Empty,
                _properties.Object, new byte[0]);

            if (!signal.Wait(Timeout))
                Assert.Fail();
        }

        [Test]
        public void ModelShutdownShouldDoNothing()
        {
            GenericSetup();

            _sut.HandleModelShutdown(It.IsAny<IModel>(),
                new ShutdownEventArgs(ShutdownInitiator.Application, 0, String.Empty));
        }

        [Test]
        public void ShouldDispose()
        {
            GenericSetup();

            _sut.Dispose();

            _model.Verify(x => x.Dispose(), Times.Once);
        }
    }
}