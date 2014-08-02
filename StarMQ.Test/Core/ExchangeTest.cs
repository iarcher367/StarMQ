namespace StarMQ.Test.Core
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Core;
    using System;

    public class ExchangeTest
    {
        private const string Expected = "StarMQ.Message:StarMQ";

        [Test]
        public void ShouldSetExchangeName()
        {
            var exchange = new Exchange(Expected);

            Assert.That(exchange.Name, Is.EqualTo(Expected));
        }

        [Test]
        public void ShouldDefaultDurableToTrue()
        {
            var exchange = new Exchange(Expected);

            Assert.That(exchange.Durable, Is.True);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfNameIsTooLong()
        {
            var exchange = new Exchange(new String('*', 256));
        }
    }
}