namespace StarMQ.Test.Publish
{
    using log4net;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using StarMQ.Publish;

    public class BasePublisherTest
    {
        private Mock<ILog> _log;
        private Mock<IModel> _model;
        private BasePublisher _sut;

        private Mock<IBasicProperties> _properties;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _model = new Mock<IModel>(MockBehavior.Strict);
            _sut = new BasicPublisher(_log.Object);

            _properties = new Mock<IBasicProperties>();
        }

        [Test]
        public void ShouldSynchronizeModelChanges()
        {
            var count = default(int);
            var oldModel = new Mock<IModel>(MockBehavior.Strict);

            _sut.BasicReturn += (o, e) => { count += 5; };
            _sut.Publish(oldModel.Object, x => { });
            _sut.Publish(_model.Object, x => { });

            oldModel.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(count, Is.EqualTo(default(int)));

            _model.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(count, Is.EqualTo(5));  // only current model has a handler
        }

        [Test]
        public void ShouldFireBasicReturnEventIfModelBasicReturnFires()
        {
            var flag = false;

            _sut.BasicReturn += (o, e) => { flag = true; };

            _sut.Publish(_model.Object, x => { });

            _model.Raise(x => x.BasicReturn += null, new BasicReturnEventArgs
            {
                BasicProperties = _properties.Object
            });

            Assert.That(flag, Is.True);
        }
    }
}