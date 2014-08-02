namespace StarMQ.Test.Message
{
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

    public class CorrelationStrategyTest
    {
        private ICorrelationStrategy _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new CorrelationStrategy();
        }

        [Test]
        public void ShouldReturnGuid()
        {
            Guid guid;
            var actual = _sut.GenerateCorrelationId();

            Assert.That(Guid.TryParse(actual, out guid));
        }
    }
}
