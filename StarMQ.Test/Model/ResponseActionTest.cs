namespace StarMQ.Test.Model
{
    using NUnit.Framework;
    using StarMQ.Model;

    public class ResponseActionTest
    {
        [Test]
        public void ShouldDefaultToDoNothing()
        {
            Assert.That(default(ResponseAction), Is.EqualTo(ResponseAction.DoNothing));
        }
    }
}