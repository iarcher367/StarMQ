namespace StarMQ.Test.Model
{
    using Exception;
    using NUnit.Framework;
    using StarMQ.Model;
    using System;

    public class QueueTest
    {
        private const string Expected = "StarMQ.Message:StarMQ";

        [Test]
        public void ShouldSetQueueName()
        {
            var queue = new Queue(Expected);

            Assert.That(queue.Name, Is.EqualTo(Expected));
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowExceptionIfNameIsNull()
        {
            var queue = new Queue(null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfNameIsTooLong()
        {
            var queue = new Queue(new String('*', 256));
        }

        [Test]
        public void ShouldDefaultDurableToTrue()
        {
            var queue = new Queue(Expected);

            Assert.That(queue.Durable, Is.True);
        }

        [Test]
        public void ShouldDefaultMessageTimeToLiveToMaxValue()
        {
            var queue = new Queue(Expected);

            Assert.That(queue.MessageTimeToLive, Is.EqualTo(uint.MaxValue));
        }
    }
}