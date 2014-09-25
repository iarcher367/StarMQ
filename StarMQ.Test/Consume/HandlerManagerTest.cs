namespace StarMQ.Test.Consume
{
    using Exception;
    using log4net;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Model;
    using System;

    public class HandlerManagerTest
    {
        private Mock<ILog> _log;
        private IHandlerManager _sut;

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _sut = new HandlerManager(_log.Object);
        }

        [Test]
        public void AddActionShouldReturnAckResponseIfNoException()
        {
            _sut.Add<Factory>(x => { });

            var handler = _sut.Get(typeof(Factory));
            var response = handler(new Factory());

            Assert.That(response, Is.TypeOf<AckResponse>());
        }

        [Test]
        public void AddActionShouldReturnNackResponseIfHandlerThrowsException()
        {
            _sut.Add<Factory>(x => { throw new Exception(); });

            var handler = _sut.Get(typeof(Factory));
            var response = handler(new Factory());

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddActionShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Factory>(x => { });
            _sut.Add<Properties>(x => { });

            Assert.That(_sut.Default, Is.EqualTo(typeof(Factory)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddActionShouldThrowExceptionIfHandlerIsNull()
        {
            _sut.Add((Action<string>)null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void AddActionShouldThrowExceptionIfTypeAlreadyRegistered()
        {
            _sut.Add<string>(x => { });
            _sut.Add<string>(x => { });
        }

        [Test]
        public void AddFuncShouldReturnNackResponseIfHandlerThrowsException()
        {
            _sut.Add<Factory>(x => { throw new Exception(); });

            var handler = _sut.Get(typeof(Factory));
            var response = handler(new Factory());

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddFuncShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Factory>(x => new AckResponse());
            _sut.Add<Properties>(x => new AckResponse());

            Assert.That(_sut.Default, Is.EqualTo(typeof(Factory)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddFuncShouldThrowExceptionIfHandlerIsNull()
        {
            _sut.Add((Func<string, BaseResponse>)null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void AddFuncShouldThrowExceptionIfTypeAlreadyRegistered()
        {
            _sut.Add<string>(x => new AckResponse());
            _sut.Add<string>(x => new AckResponse());
        }

        [Test]
        public void GetShouldReturnFuncFromAction()
        {
            _sut.Add<Factory>(x => { });

            var actual = _sut.Get(typeof(Factory));

            Assert.That(actual, Is.TypeOf<Func<Factory, BaseResponse>>());
        }

        [Test]
        public void GetShouldReturnFunc()
        {
            _sut.Add<Factory>(x => new AckResponse());

            var actual = _sut.Get(typeof(Factory));

            Assert.That(actual, Is.TypeOf<Func<Factory, BaseResponse>>());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetShouldThrowExceptionIfTypeIsNull()
        {
            _sut.Get(null);
        }

        [Test]
        public void ShouldValidate()
        {
            _sut.Add<Factory>(x => { });
            _sut.Validate();
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfNoHandlersRegistered()
        {
            _sut.Validate();
        }
    }
}