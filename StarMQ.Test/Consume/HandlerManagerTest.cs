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
    using Exception;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Consume;
    using StarMQ.Model;
    using System;

    public class HandlerManagerTest
    {
        private Mock<ILog> _log;
        private IHandlerManager _sut;

        private DeliveryContext _context;

        [SetUp]
        public void Setup()
        {
            _context = new DeliveryContext();
            _log = new Mock<ILog>();
            _sut = new HandlerManager(_log.Object);
        }

        [Test]
        public void AddActionShouldReturnAckResponseIfNoException()
        {
            _sut.Add<Helper>((x, y) => { });

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper(), _context);

            Assert.That(response, Is.TypeOf<AckResponse>());
        }

        [Test]
        public void AddActionShouldReturnNackResponseIfHandlerThrowsException()
        {
            _sut.Add((Action<Helper, DeliveryContext>)((x, y) => { throw new Exception(); }));

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper(), _context);

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddActionShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Helper>((x, y) => { });
            _sut.Add<Properties>((x, y) => { });

            Assert.That(_sut.Default, Is.EqualTo(typeof(Helper)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddActionShouldThrowExceptionIfHandlerIsNull()
        {
            _sut.Add((Action<string, DeliveryContext>)null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void AddActionShouldThrowExceptionIfTypeAlreadyRegistered()
        {
            _sut.Add<string>((x, y) => { });
            _sut.Add<string>((x, y) => { });
        }

        [Test]
        public void AddFuncShouldReturnNackResponseIfHandlerThrowsException()
        {
            _sut.Add<Helper>((x, y) => { throw new Exception(); });

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper(), _context);

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddFuncShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Helper>((x, y) => new AckResponse());
            _sut.Add<Properties>((x, y) => new AckResponse());

            Assert.That(_sut.Default, Is.EqualTo(typeof(Helper)));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddFuncShouldThrowExceptionIfHandlerIsNull()
        {
            _sut.Add((Func<string, DeliveryContext, BaseResponse>)null);
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void AddFuncShouldThrowExceptionIfTypeAlreadyRegistered()
        {
            _sut.Add<string>((x, y) => new AckResponse());
            _sut.Add<string>((x, y) => new AckResponse());
        }

        [Test]
        public void GetShouldReturnFuncFromAction()
        {
            _sut.Add<Helper>((x, y) => { });

            var actual = _sut.Get(typeof(Helper));

            Assert.That(actual, Is.TypeOf<Func<Helper, DeliveryContext, BaseResponse>>());
        }

        [Test]
        public void GetShouldReturnFunc()
        {
            _sut.Add<Helper>((x, y) => new AckResponse());

            var actual = _sut.Get(typeof(Helper));

            Assert.That(actual, Is.TypeOf<Func<Helper, DeliveryContext, BaseResponse>>());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetShouldThrowExceptionIfTypeIsNull()
        {
            _sut.Get(null);
        }

        [Test]
        public void GetShouldReturnDefaultHandlerIfTypeNotRegistered()
        {
            var expected = new NackResponse();
            Func<Helper, DeliveryContext, BaseResponse> handler = (x, y) => expected;

            _sut.Add(handler);

            var func = _sut.Get(typeof(Properties));
            var actual = func(null, _context);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void ShouldValidate()
        {
            _sut.Add<Helper>((x, y) => { });
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