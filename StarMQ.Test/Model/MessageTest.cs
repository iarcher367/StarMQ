namespace StarMQ.Test.Model
{
    using NUnit.Framework;
    using StarMQ.Model;
    using System;

    public class MessageTest
    {
        [Test]
        public void ShouldSetBody()
        {
            const string body = "Lorem ipsum dolor sit amet, qui no timeam similique.";

            var sut = new Message<string>(body);

            Assert.That(sut.Body, Is.SameAs(body));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfBodyIsNull()
        {
            var sut = new Message<object>(null);
        }

        [Test]
        public void ShouldSetProperties()
        {
            var expected = new Properties();
            var sut = new Message<string>(String.Empty) { Properties = expected };

            Assert.That(sut.Properties, Is.SameAs(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfPropertiesIsNull()
        {
            var sut = new Message<string>(String.Empty) { Properties = null };
        }
    }
}
