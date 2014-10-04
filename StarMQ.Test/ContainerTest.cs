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

namespace StarMQ.Test
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

    public class ContainerTest
    {
        private IContainer _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new Container();
        }

        #region Test classes
        internal class ConstructorStrategy : ICorrelationStrategy
        {
            public ConstructorStrategy(ILog log) { }
            public ConstructorStrategy(EmptyLog log) { }
            public string GenerateCorrelationId() { return String.Empty; }
        }

        internal class AlphaDecoratorStrategy : ICorrelationStrategy
        {
            public AlphaDecoratorStrategy(ICorrelationStrategy strategy, ILog log) { }
            public string GenerateCorrelationId() { return String.Empty; }
        }

        internal class BetaDecoratorStrategy : ICorrelationStrategy
        {
            public BetaDecoratorStrategy(ICorrelationStrategy strategy, ILog log) { }
            public string GenerateCorrelationId() { return String.Empty; }
        }

        internal class GeneratorStrategy : ICorrelationStrategy
        {
            public GeneratorStrategy(string id) { }
            public string GenerateCorrelationId() { return String.Empty; }
        }

        internal class NestedSerializer : ISerializer
        {
            public ICorrelationStrategy Strategy { get; set; }
            public NestedSerializer(ICorrelationStrategy strategy) { Strategy = strategy; }
            public byte[] ToBytes<T>(T content) where T : class { return new byte[0]; }
            public dynamic ToObject(byte[] content, Type type) { return new object(); }
        }
        #endregion

        [Test]
        public void ShouldResolveGeneratedType()
        {
            _sut.Register<ICorrelationStrategy>(x => new GeneratorStrategy(String.Empty));

            var actual = _sut.Resolve<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<GeneratorStrategy>());
        }

        [Test]
        public void ShouldResolveRegisteredType()
        {
            _sut.Register<ILog, EmptyLog>();

            var actual = _sut.Resolve<ILog>();

            Assert.That(actual, Is.TypeOf<EmptyLog>());
        }

        [Test]
        public void ShouldResolveUndecoratedTypeIfConditionFails()
        {
            _sut.Register<ILog, EmptyLog>();
            _sut.Register<ICorrelationStrategy, CorrelationStrategy>();
            _sut.RegisterDecorator<ICorrelationStrategy, AlphaDecoratorStrategy>(() => true);
            _sut.RegisterDecorator<ICorrelationStrategy, BetaDecoratorStrategy>(() => false);

            var actual = _sut.Resolve<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<AlphaDecoratorStrategy>());
        }

        [Test]
        public void ShouldResolveDecoratedTypeIfConditionPasses()
        {
            _sut.Register<ILog, EmptyLog>();
            _sut.Register<ICorrelationStrategy, CorrelationStrategy>();
            _sut.RegisterDecorator<ICorrelationStrategy, AlphaDecoratorStrategy>(() => true);

            var actual = _sut.Resolve<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<AlphaDecoratorStrategy>());
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfImplementationTooManyConstructors()
        {
            _sut.Register<ICorrelationStrategy, ConstructorStrategy>();
        }

        [Test]
        public void ShouldOverrideRegistration()
        {
            _sut.Register<ICorrelationStrategy>(x => new GeneratorStrategy(String.Empty));
            _sut.Register<ICorrelationStrategy, FactoryTest.EmptyStrategy>();

            var actual = _sut.Resolve<ICorrelationStrategy>();

            Assert.That(actual, Is.TypeOf<FactoryTest.EmptyStrategy>());
        }

        [Test]
        public void ShouldResolveSameInstanceIfSingleton()
        {
            _sut.Register<ILog, EmptyLog>(true);

            var expected = _sut.Resolve<ILog>();
            var actual = _sut.Resolve<ILog>();

            Assert.That(actual, Is.SameAs(expected));
        }

        [Test]
        public void ShouldResolveNewInstance()
        {
            _sut.Register<ILog, EmptyLog>();

            var expected = _sut.Resolve<ILog>();
            var actual = _sut.Resolve<ILog>();

            Assert.That(actual, Is.Not.SameAs(expected));
        }

        [Test]
        public void ShouldResolveNewGeneratedInstance()
        {
            _sut.Register<ILog>(x => new EmptyLog());

            var expected = _sut.Resolve<ILog>();
            var actual = _sut.Resolve<ILog>();

            Assert.That(actual, Is.Not.SameAs(expected));
        }

        [Test]
        public void ShouldResolveRegisteredTypeWithGeneratedConstructorArgument()
        {
            _sut.Register<ICorrelationStrategy>(x => new GeneratorStrategy(String.Empty));
            _sut.Register<ISerializer, NestedSerializer>();

            var actual = _sut.Resolve<ISerializer>();

            Assert.That(actual, Is.TypeOf<NestedSerializer>());
        }

        [Test]
        public void ShouldResolveRegisteredTypeWithDecoratedConstructorArgument()
        {
            _sut.Register<ILog, EmptyLog>();
            _sut.Register<ISerializer, NestedSerializer>();
            _sut.Register<ICorrelationStrategy, CorrelationStrategy>();
            _sut.RegisterDecorator<ICorrelationStrategy, AlphaDecoratorStrategy>(() => true);

            var actual = _sut.Resolve<ISerializer>();

            Assert.That(actual, Is.TypeOf<NestedSerializer>());
            Assert.That(((NestedSerializer)actual).Strategy, Is.TypeOf<AlphaDecoratorStrategy>());
        }

        [Test]
        [ExpectedException(typeof(StarMqException))]
        public void ShouldThrowExceptionIfTypeNotFound()
        {
            _sut.Resolve<ILog>();
        }
    }
}