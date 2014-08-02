namespace StarMQ.Test.Message
{
    using NUnit.Framework;
    using StarMQ.Message;
    using System;

    public class PropertiesTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfTargetIsNull()
        {
            var prop = new Properties();

            prop.CopyTo(null);
        }

        [Test]
        public void ShouldCheckEveryPropertySetterAndCopyTo()
        {
            Assert.Fail();
        }
    }
}