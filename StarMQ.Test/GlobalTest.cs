namespace StarMQ.Test
{
    using Exception;
    using Moq;
    using NUnit.Framework;
    using StarMQ.Core;
    using System;

    public class GlobalTest
    {
        private const string ConnectionString = "host=space;username=sol;password=luna";

        private Mock<IConnectionConfiguration> _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = new Mock<IConnectionConfiguration>(MockBehavior.Strict);
        }

        [Test]
        public void ShouldParseConfiguration()
        {
            Global.ParseConfiguration(_configuration.Object, ConnectionString);

            Assert.Fail();
        }

        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ShouldThrowExceptionIfConfigurationIsNull()
        {
            Global.ParseConfiguration(null, String.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfConnectionStringIsNull()
        {
            Global.ParseConfiguration(new ConnectionConfiguration(), null);
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