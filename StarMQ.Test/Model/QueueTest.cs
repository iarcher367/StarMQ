namespace StarMQ.Test.Model
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Model;
    using System;
    using System.Linq;

    public class QueueTest
    {
        private const string Expected = "StarMQ.Message:StarMQ";

        [Test]
        public void ShouldDefaultDurableToTrue()
        {
            var queue = new Queue();

            Assert.That(queue.Durable, Is.True);
        }

        [Test]
        public void ShouldDefaultMessageTimeToLiveToMaxValue()
        {
            var queue = new Queue();

            Assert.That(queue.MessageTimeToLive, Is.EqualTo(uint.MaxValue));
        }

        #region Fluent
        [Test]
        public void ShouldSetQueueName()
        {
            var queue = new Queue().WithName(Expected);

            Assert.That(queue.Name, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfNameIsNull()
        {
            new Queue().WithName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfNameIsTooLong()
        {
            new Queue().WithName(new String('*', 256));
        }

        [Test]
        public void ShouldSetAutoDelete()
        {
            var actual = new Queue().WithAutoDelete(true).AutoDelete;

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldSetDurable()
        {
            var actual = new Queue().WithDurable(false).Durable;

            Assert.That(actual, Is.False);
        }

        [Test]
        public void ShouldSetExclusive()
        {
            var actual = new Queue().WithExclusive(true).Exclusive;

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldSetCancelOnHaFailover()
        {
            var actual = new Queue().WithCancelOnHaFailover(true).CancelOnHaFailover;

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldSetDeadLetterExchangeName()
        {
            const string expected = "StarMQ.DLX";
            var actual = new Queue().WithDeadLetterExchangeName(expected).DeadLetterExchangeName;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfDeadLetterExchangeNameIsNull()
        {
            new Queue().WithDeadLetterExchangeName(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfDeadLetterExchangeNameIsTooLong()
        {
            new Queue().WithDeadLetterExchangeName(new String('*', 256));
        }

        [Test]
        public void ShouldSetDeadLetterRoutingKey()
        {
            const string expected = "StarMQ.*";
            var actual = new Queue().WithDeadLetterRoutingKey(expected).DeadLetterRoutingKey;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfRoutingKeyIsNull()
        {
            new Queue().WithDeadLetterRoutingKey(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfRoutingKeyIsTooLong()
        {
            new Queue().WithDeadLetterRoutingKey(new String('*', 256));
        }

        [Test]
        public void ShouldSetExpires()
        {
            const uint expected = 7;
            var actual = new Queue().WithExpires(expected).Expires;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldSetMessageTimeToLive()
        {
            const uint expected = 16;
            var actual = new Queue().WithMessageTimeToLive(expected).MessageTimeToLive;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldSetPriority()
        {
            const int expected = -1;
            var actual = new Queue().WithPriority(expected).Priority;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldAddBindingKey()
        {
            const string expected = "StarMQ.#";

            var keys = new Queue().WithBindingKey(expected).BindingKeys;

            Assert.That(keys.First(), Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ShouldThrowExceptionIfBindingKeyIsNull()
        {
            new Queue().WithBindingKey(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ShouldThrowExceptionIfBindingKeyIsEmpty()
        {
            new Queue().WithBindingKey(String.Empty);
        }
        #endregion
    }
}