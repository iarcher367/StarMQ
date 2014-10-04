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

        [SetUp]
        public void Setup()
        {
            _log = new Mock<ILog>();
            _sut = new HandlerManager(_log.Object);
        }

        [Test]
        public void AddActionShouldReturnAckResponseIfNoException()
        {
            _sut.Add<Helper>(x => { });

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper());

            Assert.That(response, Is.TypeOf<AckResponse>());
        }

        [Test]
        public void AddActionShouldReturnNackResponseIfHandlerThrowsException()
        {
            _sut.Add((Action<Helper>)(x => { throw new Exception(); }));

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper());

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddActionShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Helper>(x => { });
            _sut.Add<Properties>(x => { });

            Assert.That(_sut.Default, Is.EqualTo(typeof(Helper)));
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
            _sut.Add<Helper>(x => { throw new Exception(); });

            var handler = _sut.Get(typeof(Helper));
            var response = handler(new Helper());

            Assert.That(response, Is.TypeOf<NackResponse>());
        }

        [Test]
        public void AddFuncShouldSetFirstRegistrationAsDefaultType()
        {
            _sut.Add<Helper>(x => new AckResponse());
            _sut.Add<Properties>(x => new AckResponse());

            Assert.That(_sut.Default, Is.EqualTo(typeof(Helper)));
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
            _sut.Add<Helper>(x => { });

            var actual = _sut.Get(typeof(Helper));

            Assert.That(actual, Is.TypeOf<Func<Helper, BaseResponse>>());
        }

        [Test]
        public void GetShouldReturnFunc()
        {
            _sut.Add<Helper>(x => new AckResponse());

            var actual = _sut.Get(typeof(Helper));

            Assert.That(actual, Is.TypeOf<Func<Helper, BaseResponse>>());
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
            Func<Helper, BaseResponse> handler = x => expected;

            _sut.Add(handler);

            var func = _sut.Get(typeof(Properties));
            var actual = func(null);

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void ShouldValidate()
        {
            _sut.Add<Helper>(x => { });
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