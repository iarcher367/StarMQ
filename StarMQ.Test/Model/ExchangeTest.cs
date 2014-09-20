namespace StarMQ.Test.Model
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Model;
    using System;

    public class ExchangeTest
    {
        private const string Expected = "StarMQ.Message:StarMQ";

        [Test]
        public void ShouldDefaultDurableToTrue()
        {
            var exchange = new Exchange();

            Assert.That(exchange.Durable, Is.True);
        }

        [Test]
        public void ShouldSetExchangeName()
        {
            var exchange = new Exchange().WithName(Expected);

            Assert.That(exchange.Name, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfNameIsNull()
        {
            new Exchange().WithName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfNameIsTooLong()
        {
            new Exchange().WithName(new String('*', 256));
        }

        [Test]
        public void ShouldSetAutoDelete()
        {
            var actual = new Exchange().WithAutoDelete(true).AutoDelete;

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldSetDurable()
        {
            var actual = new Exchange().WithDurable(false).Durable;

            Assert.That(actual, Is.False);
        }

        [Test]
        public void ShouldSetAlternateExchangeName()
        {
            var actual = new Exchange().WithAlternateExchangeName(Expected).AlternateExchangeName;

            Assert.That(actual, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfAlternateExchangeNameIsNull()
        {
            new Exchange().WithAlternateExchangeName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfAlternateExchangeNameIsTooLong()
        {
            new Exchange().WithAlternateExchangeName(new String('*', 256));
        }

    }
}