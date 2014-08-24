namespace StarMQ.Test
{
    using NUnit.Framework;
    using System;

    public class SimpleBusFactoryTest
    {
        [Test]
        public void ShouldGetBus()
        {
            var actual = SimpleBusFactory.GetBus();
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public void ShouldGetBusWithConnectionString()
        {
            var actual = SimpleBusFactory.GetBus(String.Empty);

            Assert.That(actual, Is.Not.Null);
            Assert.Inconclusive();
        }
    }
}