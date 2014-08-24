namespace StarMQ.Test
{
    using Exception;
    using NUnit.Framework;
    using System;

    public class GlobalTest
    {
        [Test]
        public void ShouldParseConfiguration()
        {
            Assert.Fail();
        }

        [Test]
        public void ShouldReturnValue()
        {
            const string value = "StarMQ.Master";

            var actual = Global.Validate("name", value);

            Assert.That(actual, Is.EqualTo(value));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ValidateShouldThrowExceptionIfFieldIsNull()
        {
            Global.Validate(String.Empty, null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ValidateShouldThrowExceptionIfValueIsNull()
        {
            Global.Validate(String.Empty, null);
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ValidateShouldThrowExceptionIfValueIsTooLong()
        {
            Global.Validate(String.Empty, new string('*', 256));
        }
    }
}