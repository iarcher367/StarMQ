namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Publish;
    using IConnection = StarMQ.Core.IConnection;

    public class BasePublisherTest
    {
        private Mock<IConnection> _connection;
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private BasePublisher _sut;

        private Mock<IBasicProperties> _properties;

        [SetUp]
        public void Setup()
        {
            _connection = new Mock<IConnection>();
            _log = new Mock<ILog>();
            _model = new Mock<IModel>();

            _connection.Setup(x => x.CreateModel()).Returns(_model.Object);

            _sut = new BasicPublisher(_connection.Object, _log.Object);

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