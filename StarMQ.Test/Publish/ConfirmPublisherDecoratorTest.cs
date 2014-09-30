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
    using Exception;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Core;
    using StarMQ.Model;
    using StarMQ.Publish;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class ConfirmPublisherDecoratorTest
    {
        private const int Timeout = 40;
        private const int Offset = 10;
        private readonly ulong[] _seqNos = { 13, 14, 15, 16, 17, 18 };

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private Mock<IPublisher> _publisher;
        private ConfirmPublisherDecorator _sut;

        private IMessage<string> _message;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();
            _publisher = new Mock<IPublisher>();

            _configuration.Setup(x => x.Timeout).Returns(Timeout);
            _model.SetupSequence(x => x.NextPublishSeqNo)
                .Returns(_seqNos[0])
                .Returns(_seqNos[1])
                .Returns(_seqNos[2])
                .Returns(_seqNos[3])
                .Returns(_seqNos[4])
                .Returns(_seqNos[5]);
            _publisher.Setup(x => x.Model).Returns(_model.Object);

            _sut = new ConfirmPublisherDecorator(_publisher.Object, _configuration.Object,
                _connection.Object, _log.Object);

            _message = new Message<string>(String.Empty);
        }

        [Test]
        public void ShouldBasicReturn()
        {
            var flag = false;

            _sut.BasicReturn += (o, e) => flag = true;

            _publisher.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs());

            Assert.That(flag, Is.True);
        }

        [Test]
        public async Task ShouldBindBasicAcks()
        {
            var task = _sut.Publish(_message, (x, y, z) => { });

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;
        }

        [Test]
        [ExpectedException(typeof(PublishException))]
        public async Task ShouldBindBasicNacks()
        {
            var task = _sut.Publish(_message, (x, y, z) => { });

            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            await task;
        }

        [Test]
        public void ShouldSetConfirmSelect()
        {
            _model.Verify(x => x.ConfirmSelect(), Times.Once);
        }

        [Test]
        public async Task ShouldUnbindBasicAcksIfOnDisconnectedFires()
        {
            _connection.SetupSequence(x => x.IsConnected)
                .Returns(false)
                .Returns(true);

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(_message, (x, y, z) => { });

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            if (await Task.WhenAny(task, Task.Delay(Timeout)) == task)
                Assert.Fail("Publish should not have completed.");

            _connection.Raise(x => x.OnConnected += null);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });

            await task;

            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(2));
        }

        [Test]
        [ExpectedException(typeof(PublishException))]
        public async Task ShouldUnbindBasicNacksIfOnDisconnectedFires()
        {
            _connection.SetupSequence(x => x.IsConnected)
                .Returns(false)
                .Returns(true);

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(_message, (x, y, z) => { });

            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            if (await Task.WhenAny(task, Task.Delay(Timeout)) == task)
                Assert.Fail("Publish should not have completed.");

            _connection.Raise(x => x.OnConnected += null);

            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[1] });

            await task;

            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(2));
        }

        [Test]
        public async Task ShouldDisposeTimersIfOnDisconnectedFires()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var task = _sut.Publish(_message, (x, y, z) => { });

            _connection.Raise(x => x.OnDisconnected += null);

            await Task.Delay(Timeout * 3);

            _connection.Raise(x => x.OnConnected += null);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });

            await task;

            _connection.Verify(x => x.IsConnected, Times.Never);
            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(2));
        }

        [Test]
        public void ShouldRequeuePendingMessagesIfOnConnectedFires()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var tasks = new List<Task>
            {
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { })
            };

            _connection.Raise(x => x.OnDisconnected += null);
            _connection.Raise(x => x.OnConnected += null);

            tasks.Add(_sut.Publish(_message, (x, y, z) => { }));

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[2] });
            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[3] });
            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });

            Task.WaitAll(tasks.ToArray());

            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(5));
        }

        [Test]
        public async Task ShouldRequeuePendingMessagesInSequenceOrderWithoutDuplicates()
        {
            _connection.SetupSequence(x => x.IsConnected)
                .Returns(true)
                .Returns(false);

            var order = 0;
            var tasks = new List<Task>
            {
                _sut.Publish(_message, (x, y, z) => x.BasicCancel("1"))
            };

            await Task.Delay(Offset);

            tasks.Add(_sut.Publish(_message, (x, y, z) => x.BasicCancel("2")));

            await Task.Delay(Timeout);

            _connection.Raise(x => x.OnDisconnected += null);

            _model.Setup(x => x.BasicCancel("1"))
                .Callback(() => Assert.That(order++, Is.EqualTo(0)));
            _model.Setup(x => x.BasicCancel("2"))
                .Callback(() => Assert.That(order++, Is.EqualTo(1)));

            _connection.Raise(x => x.OnConnected += null);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[3] });
            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });

            Task.WaitAll(tasks.ToArray());

            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(5));
        }

        [Test]
        public void ShouldAvoidReentrancyIssuesOnMappingStateToSequenceId()
        {
            Assert.Inconclusive();
        }

        [Test]
        public async Task ShouldInvokeAndConfirmOnBasicAck()
        {
            Action<IModel, IBasicProperties, byte[]> action = (x, y, z) => { };
            var flag = false;

            _publisher.Setup(x => x.Publish(It.IsAny<IMessage<string>>(), It.IsAny<Action<IModel, IBasicProperties, byte[]>>()))
                .Callback<IMessage<string>, Action<IModel, IBasicProperties, byte[]>>((a, x) => action = x);

            var task = _sut.Publish(_message, (x, y, z) => flag = true);

            action(_model.Object, new Mock<IBasicProperties>().Object, new byte[0]);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldInvokeAndConfirmMultipleOnBasicAckWithMultipleSet()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var tasks = new List<Task>
            {
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { })
            };

            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[1] });
            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs
            {
                DeliveryTag = _seqNos[2],
                Multiple = true
            });
            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[3] });

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException agg)
            {
                Assert.That(agg.InnerExceptions.Count, Is.EqualTo(2));
            }
        }

        [Test]
        [ExpectedException(typeof(PublishException))]
        public async Task ShouldInvokeAndThrowExceptionOnBasicNack()
        {
            var task = _sut.Publish(_message, (x, y, z) => { });

            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            await task;
        }

        [Test]
        public void ShouldInvokeAndThrowMultipleExceptionsOnBasicNackWithMultipleSet()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var tasks = new List<Task>
            {
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { }),
                _sut.Publish(_message, (x, y, z) => { })
            };

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });
            _model.Raise(x => x.BasicNacks += null, new BasicNackEventArgs
            {
                DeliveryTag = _seqNos[2],
                Multiple = true
            });
            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[3] });

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException agg)
            {
                Assert.That(agg.InnerExceptions.Count, Is.EqualTo(2));

                foreach (var ex in agg.InnerExceptions)
                    Assert.That(ex, Is.TypeOf<PublishException>());
            }
        }

        [Test]
        public async Task ShouldInvokeAndRepublishOnTimeout()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var task = _sut.Publish(_message, (x, y, z) => { });

            await Task.Delay(Timeout + Offset);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });

            await task;

            _model.Verify(x => x.NextPublishSeqNo, Times.Exactly(2));
        }

        [Test]
        public async Task ShouldNotRepublishOnTimeoutIfDisconnected()
        {
            _connection.Setup(x => x.IsConnected).Returns(false);

            var task = _sut.Publish<string>(null, (x, y, z) => { });

            await Task.Delay(Timeout + Offset);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            _model.Verify(x => x.NextPublishSeqNo, Times.Once);
        }

        [Test]
        public async Task ShouldInvokeActionAndProcessMessage()
        {
            var publishAction = new Mock<Action<IModel, IBasicProperties, byte[]>>();

            var task = _sut.Publish(_message, publishAction.Object);

            _model.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            _publisher.Verify(x => x.Publish(_message, publishAction.Object), Times.Once);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ShouldThrowExceptionIfActionIsNull()
        {
            await _sut.Publish<string>(null, null);
        }

        [Test]
        public void ShouldDispose()
        {
            _sut.Dispose();

            _publisher.Verify(x => x.Dispose(), Times.Once);
        }
    }
}