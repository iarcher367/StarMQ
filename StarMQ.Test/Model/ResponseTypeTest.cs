namespace StarMQ.Test.Model
{
    using NUnit.Framework;
    using StarMQ.Model;

    public class ResponseTypeTest
    {
        [Test]
        public void ShouldDefaultToAck()
        {
            Assert.That(default(ResponseType), Is.EqualTo(ResponseType.Ack));
        }
    }
}