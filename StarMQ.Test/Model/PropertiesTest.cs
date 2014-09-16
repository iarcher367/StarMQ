namespace StarMQ.Test.Model
{
    using Exception;
    using Moq;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using StarMQ.Model;
    using System;
    using System.Collections.Generic;

    public class PropertiesTest
    {
        private Mock<IBasicProperties> _basicProperties;
        private Properties _sut;

        [SetUp]
        public void Setup()
        {
            _basicProperties = new Mock<IBasicProperties>();
            _sut = new Properties();
        }

        #region ContentEncoding
        [Test]
        public void ShouldSetContentEncoding()
        {
            const string expected = "UTF-8";
            _sut.ContentEncoding = expected;

            Assert.That(_sut.ContentEncoding, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfContentEncodingIsNull()
        {
            _sut.ContentEncoding = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfContentEncodingIsTooLong()
        {
            _sut.ContentEncoding = new String('*', 256);
        }
        #endregion

        #region ContentType
        [Test]
        public void ShouldSetContentType()
        {
            const string expected = "application/json";
            _sut.ContentType = expected;

            Assert.That(_sut.ContentType, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfContentTypeIsNull()
        {
            _sut.ContentType = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfContentTypeIsTooLong()
        {
            _sut.ContentType = new String('*', 256);
        }
        #endregion

        #region CorrelationId
        [Test]
        public void ShouldSetCorrelationId()
        {
            var expected = Guid.NewGuid().ToString();
            _sut.CorrelationId = expected;

            Assert.That(_sut.CorrelationId, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfCorrelationIdIsNull()
        {
            _sut.CorrelationId = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfCorrelationIdIsTooLong()
        {
            _sut.CorrelationId = new String('*', 256);
        }
        #endregion

        #region DeliveryMode
        [Test]
        public void ShouldSetDeliveryModeTransient()
        {
            const byte expected = 1;
            _sut.DeliveryMode = expected;

            Assert.That(_sut.DeliveryMode, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldSetDeliveryModePersistent()
        {
            const byte expected = 2;
            _sut.DeliveryMode = expected;

            Assert.That(_sut.DeliveryMode, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(InvalidValueException))]
        public void ShouldThrowExceptionIfDeliveryModeIsZero()
        {
            _sut.DeliveryMode = 0;
        }

        [Test]
        [ExpectedException(typeof(InvalidValueException))]
        public void ShouldThrowExceptionIfDeliveryModeIsMoreThanTwo()
        {
            _sut.DeliveryMode = 3;
        }
        #endregion

        [Test]
        public void ShouldSetHeaders()
        {
            var headers = new Dictionary<string, object>
                {
                    { "hello", "world" }
                };

            _sut.Headers = headers;

            Assert.That(_sut.Headers, Is.SameAs(headers));
        }

        #region MessageId
        [Test]
        public void ShouldSetMessageId()
        {
            var expected = Guid.NewGuid().ToString();
            _sut.MessageId = expected;

            Assert.That(_sut.MessageId, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfMessageIdIsNull()
        {
            _sut.MessageId = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfMessageIdIsTooLong()
        {
            _sut.MessageId = new String('*', 256);
        }
        #endregion

        #region Priority
        [Test]
        public void ShouldSetPriorityZero()
        {
            const byte expected = 0;
            _sut.Priority = expected;

            Assert.That(_sut.Priority, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldSetPriorityNine()
        {
            const byte expected = 9;
            _sut.Priority = expected;

            Assert.That(_sut.Priority, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(InvalidValueException))]
        public void ShouldThrowExceptionIfPriorityIsTen()
        {
            _sut.Priority = 10;
        }
        #endregion

        #region ReplyTo
        [Test]
        public void ShouldSetReplyTo()
        {
            const string expected = "unique.routing.key";
            _sut.ReplyTo = expected;

            Assert.That(_sut.ReplyTo, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfReplyToIsNull()
        {
            _sut.ReplyTo = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfReplyToIsTooLong()
        {
            _sut.ReplyTo = new String('*', 256);
        }
        #endregion

        #region Type
        [Test]
        public void ShouldSetType()
        {
            const string expected = "StarMQ.Model.Properties";
            _sut.Type = expected;

            Assert.That(_sut.Type, Is.EqualTo(expected));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfTypeIsNull()
        {
            _sut.Type = null;
        }

        [Test]
        [ExpectedException(typeof(MaxLengthException))]
        public void ShouldThrowExceptionIfTypeIsTooLong()
        {
            _sut.Type = new String('*', 256);
        }
        #endregion

        #region CopyFrom
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CopyFromShouldThrowExceptionIfTargetIsNull()
        {
            _sut.CopyFrom(null);
        }

        [Test]
        public void CopyFromShouldSetContentEncoding()
        {
            const string expected = "UTF-8";

            _basicProperties.Setup(x => x.IsContentEncodingPresent()).Returns(true);
            _basicProperties.Setup(x => x.ContentEncoding).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ContentEncoding, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetContentEncodingIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ContentEncoding, Is.Null);
        }

        [Test]
        public void CopyFromShouldSetContentType()
        {
            const string expected = "application/json";

            _basicProperties.Setup(x => x.IsContentTypePresent()).Returns(true);
            _basicProperties.Setup(x => x.ContentType).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ContentType, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetContentTypeIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ContentType, Is.Null);
        }

        [Test]
        public void CopyFromShouldSetCorrelationId()
        {
            var expected = Guid.NewGuid().ToString();

            _basicProperties.Setup(x => x.IsCorrelationIdPresent()).Returns(true);
            _basicProperties.Setup(x => x.CorrelationId).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.CorrelationId, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetCorrelationIdIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.CorrelationId, Is.Null);
        }

        [Test]
        public void CopyFromShouldSetDeliveryMode()
        {
            const byte expected = 1;

            _basicProperties.Setup(x => x.IsDeliveryModePresent()).Returns(true);
            _basicProperties.Setup(x => x.DeliveryMode).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.DeliveryMode, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetDeliveryModeIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.DeliveryMode, Is.EqualTo(0));
        }

        [Test]
        public void CopyFromShouldSetHeaders()
        {
            var expected = new Dictionary<string, object>();

            _basicProperties.Setup(x => x.IsHeadersPresent()).Returns(true);
            _basicProperties.Setup(x => x.Headers).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Headers, Is.SameAs(expected));
        }

        [Test]
        public void CopyFromShouldNotSetHeadersIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Headers, Is.Empty);
        }

        [Test]
        public void CopyFromShouldSetMessageId()
        {
            var expected = Guid.NewGuid().ToString();

            _basicProperties.Setup(x => x.IsMessageIdPresent()).Returns(true);
            _basicProperties.Setup(x => x.MessageId).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.MessageId, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetMessageIdIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.MessageId, Is.Null);
        }

        [Test]
        public void CopyFromShouldSetPriority()
        {
            const byte expected = 1;

            _basicProperties.Setup(x => x.IsPriorityPresent()).Returns(true);
            _basicProperties.Setup(x => x.Priority).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Priority, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetPriorityIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Priority, Is.EqualTo(0));
        }

        [Test]
        public void CopyFromShouldSetReplyTo()
        {
            const string expected = "unique.routing.key";

            _basicProperties.Setup(x => x.IsReplyToPresent()).Returns(true);
            _basicProperties.Setup(x => x.ReplyTo).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ReplyTo, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetReplyToIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.ReplyTo, Is.Null);
        }

        [Test]
        public void CopyFromShouldSetType()
        {
            const string expected = "StarMQ.Model.Properties";

            _basicProperties.Setup(x => x.IsTypePresent()).Returns(true);
            _basicProperties.Setup(x => x.Type).Returns(expected);

            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Type, Is.EqualTo(expected));
        }

        [Test]
        public void CopyFromShouldNotSetTypeIfNotSet()
        {
            _sut.CopyFrom(_basicProperties.Object);

            Assert.That(_sut.Type, Is.Null);
        }
        #endregion

        #region CopyTo
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CopyToShouldThrowExceptionIfTargetIsNull()
        {
            _sut.CopyTo(null);
        }

        [Test]
        public void CopyToShouldSetContentEncoding()
        {
            _sut.ContentEncoding = "UTF-8";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentEncoding = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetContentEncodingIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentEncoding = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetContentType()
        {
            _sut.ContentType = "application/json";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentType = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetContentTypeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentType = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetCorrelationId()
        {
            _sut.CorrelationId = Guid.NewGuid().ToString();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.CorrelationId = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetCorrelationIdIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.CorrelationId = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetDeliveryMode()
        {
            _sut.DeliveryMode = 1;

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.DeliveryMode = It.IsAny<byte>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetDeliveryModeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.DeliveryMode = It.IsAny<byte>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetHeadersIfReplaced()
        {
            _sut.Headers = new Dictionary<string, object>();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Headers = It.IsAny<Dictionary<string, object>>(), Times.Once);
        }

        [Test]
        public void CopyToShouldSetHeadersIfModified()
        {
            _sut.Headers.Add("StarMQ", 5);

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Headers = It.IsAny<Dictionary<string, object>>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetHeadersIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Headers = It.IsAny<Dictionary<string, object>>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetMessageId()
        {
            _sut.MessageId = Guid.NewGuid().ToString();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.MessageId = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetMessageIdIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.MessageId = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetPriority()
        {
            _sut.Priority = 0;

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Priority = It.IsAny<byte>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetPriorityIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Priority = It.IsAny<byte>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetReplyTo()
        {
            _sut.ReplyTo = "unique.routing.key";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ReplyTo = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetReplyToIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ReplyTo = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void CopyToShouldSetType()
        {
            _sut.Type = "StarMQ.Model.Properties";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Type = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void CopyToShouldNotSetTypeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Type = It.IsAny<string>(), Times.Never);
        }
        #endregion
    }
}