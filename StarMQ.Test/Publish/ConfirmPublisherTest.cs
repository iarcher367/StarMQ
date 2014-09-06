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

            _configuration.Setup(x => x.Timeout).Returns(1);    // TODO: set to 0.05 seconds
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
        public void ShouldSetConfirmSelectIfOnConnectedFires()
        {
            _modelOne.Verify(x => x.ConfirmSelect(), Times.Once);
        }

        [Test]
        public void ShouldRequeuePendingMessagesIfOnConnectedFires()
        {
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
        [ExpectedException(typeof(TimeoutException))]
        public async Task ShouldUnbindBasicAcksIfOnDisconnectedFires()
        {
            var flag = false;

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicAcks += null, new BasicAckEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
        }

        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public async Task ShouldUnbindBasicNacksIfOnDisconnectedFires()
        {
            var flag = false;

            _connection.Raise(x => x.OnDisconnected += null);

            var task = _sut.Publish(x => flag = true);

            _modelOne.Raise(x => x.BasicNacks += null, new BasicNackEventArgs { DeliveryTag = _seqNos[0] });

            await task;

            Assert.That(flag, Is.True);
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
        [ExpectedException(typeof(TimeoutException))]
        public async Task ShouldInvokeAndThrowExceptionOnTimeout()
        {
            _configuration.Setup(x => x.Timeout).Returns(0);    // TODO: delete once is 0.05s

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