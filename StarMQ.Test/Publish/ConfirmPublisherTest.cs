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
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Core;
    using StarMQ.Publish;
    using System;
    using System.Threading.Tasks;
    using IConnection = StarMQ.Core.IConnection;

    public class ConfirmPublisherTest
    {
        private const int Timeout = 40;
        private const int Offset = 10;
        private readonly ulong[] _seqNos = { 13, 14, 15, 16, 42, 43, 44 };

        private Mock<IConnectionConfiguration> _configuration;
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _modelOne;
        private Mock<IModel> _modelTwo;
        private ConfirmPublisher _sut;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>();
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _modelOne = new Mock<IModel>();
            _modelTwo = new Mock<IModel>();

            _configuration.Setup(x => x.Timeout).Returns(Timeout);
            _connection.SetupSequence(x => x.CreateModel())
                .Returns(_modelOne.Object)
                .Returns(_modelTwo.Object);
            _modelOne.SetupSequence(x => x.NextPublishSeqNo)
                .Returns(_seqNos[0])
                .Returns(_seqNos[1])
                .Returns(_seqNos[2])
                .Returns(_seqNos[3]);
            _modelTwo.SetupSequence(x => x.NextPublishSeqNo)
                .Returns(_seqNos[4])
                .Returns(_seqNos[5])
                .Returns(_seqNos[6]);

            _sut = new ConfirmPublisher(_configuration.Object, _connection.Object, _log.Object);
        }

        [Test]
        public async Task ShouldBindBasicAcks()
        {
            var flag = false;

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(PublishException))]
        public async Task ShouldBindBasicNacks()
        {
            var flag = false;

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldSetConfirmSelect()
        {
            _modelOne.Verify(x => x.ConfirmSelect(), Times.Once);
        }

        [Test]
        public async Task ShouldUnbindBasicAcksIfOnDisconnectedFires()
        {
            _connection.SetupSequence(x => x.IsConnected)
                .Returns(false)
                .Returns(true);

            var count = 0;

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(x => count += 2);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            if (await Task.WhenAny(task, Task.Delay(Timeout)) == task)
                Assert.Fail("Publish should not have completed.");

            _connection.Raise(x => x.OnConnected += null);

            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });

            await task;

            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        [ExpectedException(typeof(PublishException))]
        public async Task ShouldUnbindBasicNacksIfOnDisconnectedFires()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var count = 0;

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(x => count += 2);

            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            if (await Task.WhenAny(task, Task.Delay(Timeout)) == task)
                Assert.Fail("Publish should not have completed.");

            _connection.Raise(x => x.OnConnected += null);
            _modelTwo.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[4] });

            await task;
        }

        [Test]
        public async Task ShouldDisposeTimersIfOnDisconnectedFires()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var task = _sut.Publish(x => { });

            _connection.Raise(x => x.OnDisconnected += null);

            await Task.Delay(Timeout * 3);

            _connection.Raise(x => x.OnConnected += null);

            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });

            await task;

            _connection.Verify(x => x.IsConnected, Times.Never);
        }

        [Test]
        public void ShouldRequeuePendingMessagesIfOnConnectedFires()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            const int a = 3, b = 5, c = 7;
            var count = 0;

            var tasks = new[]
                {
                    _sut.Publish(x => count += a),
                    _sut.Publish(x => count += b)
                };

            _connection.Raise(x => x.OnDisconnected += null);
            _connection.Raise(x => x.OnConnected += null);

            tasks = new[]
                {
                    tasks[0],
                    tasks[1],
                    _sut.Publish(x => count += c)
                };

            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });
            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[5] });
            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[6] });

            Task.WaitAll(tasks);

            Assert.That(count, Is.EqualTo(2 * a + 2 * b + c));
        }

        [Test]
        public async Task ShouldRequeuePendingMessagesInSequenceOrderWithoutDuplicates()
        {
            _connection.SetupSequence(x => x.IsConnected)
                .Returns(true)
                .Returns(false);

            const int a = 3, b = 5;
            int order = 0, count = 0;
            var tasks = new Task[2];

            tasks[0] = _sut.Publish(x =>
            {
                x.BasicCancel("1");
                count += a;
            });

            await Task.Delay(Offset);

            tasks[1] = _sut.Publish(x =>
            {
                x.BasicCancel("2");
                count += b;
            });

            await Task.Delay(Timeout);

            _connection.Raise(x => x.OnDisconnected += null);

            _modelTwo.Setup(x => x.BasicCancel("1"))
                .Callback(() => Assert.That(order++, Is.EqualTo(0)));
            _modelTwo.Setup(x => x.BasicCancel("2"))
                .Callback(() => Assert.That(order++, Is.EqualTo(1)));

            _connection.Raise(x => x.OnConnected += null);

            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[4] });
            _modelTwo.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[5] });

            Task.WaitAll(tasks);

            Assert.That(count, Is.EqualTo(3 * a + 2 * b));
        }

        [Test]
        public void ShouldAvoidReentrancyIssuesOnMappingStateToSequenceId()
        {
            Assert.Inconclusive();
        }

        [Test]
        public async Task ShouldInvokeAndConfirmOnBasicAck()
        {
            var flag = false;

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldInvokeAndConfirmMultipleOnBasicAckWithMultipleSet()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var count = 0;
            var tasks = new[]
                {
                    _sut.Publish(x => count += 7),
                    _sut.Publish(x => count += 9),
                    _sut.Publish(x => count += 11),
                    _sut.Publish(x => count += 13)
                };

            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[1] });
            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs
                {
                    DeliveryTag = _seqNos[2],
                    Multiple = true
                });
            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[3] });

            try
            {
                Task.WaitAll(tasks);
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
            var flag = false;

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        public void ShouldInvokeAndThrowMultipleExceptionsOnBasicNackWithMultipleSet()
        {
            _connection.Setup(x => x.IsConnected).Returns(true);

            var count = 0;
            var tasks = new[]
                {
                    _sut.Publish(x => count += 7),
                    _sut.Publish(x => count += 9),
                    _sut.Publish(x => count += 11),
                    _sut.Publish(x => count += 13)
                };

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });
            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs
                {
                    DeliveryTag = _seqNos[2],
                    Multiple = true
                });
            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[3] });

            try
            {
                Task.WaitAll(tasks);
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

            var count = 0;

            var task = _sut.Publish(x => count += 2);

            await Task.Delay(Timeout + Offset);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[1] });

            await task;

            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public async Task ShouldNotRepublishOnTimeoutIfDisconnected()
        {
            _connection.Setup(x => x.IsConnected).Returns(false);

            var count = 0;

            var task = _sut.Publish(x => count += 2);

            await Task.Delay(Timeout + Offset);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfActionIsNull()
        {
            _sut.Publish(null);
        }
    }
}