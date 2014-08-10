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
            var headers = new Dictionary<string, object>();

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

        #region CopyTo
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShouldThrowExceptionIfTargetIsNull()
        {
            _sut.CopyTo(null);
        }

        [Test]
        public void ShouldCopyContentEncoding()
        {
            _sut.ContentEncoding = "UTF-8";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentEncoding = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyContentEncodingIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentEncoding = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void ShouldCopyContentType()
        {
            _sut.ContentType = "application/json";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentType = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyContentTypeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ContentType = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void ShouldCopyCorrelationId()
        {
            _sut.CorrelationId = Guid.NewGuid().ToString();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.CorrelationId = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyCorrelationIdIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.CorrelationId = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void ShouldCopyDeliveryMode()
        {
            _sut.DeliveryMode = 1;

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.DeliveryMode = It.IsAny<byte>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyDeliveryModeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.DeliveryMode = It.IsAny<byte>(), Times.Never);
        }

        [Test]
        public void ShouldCopyHeaders()
        {
            _sut.Headers = new Dictionary<string, object>();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Headers = It.IsAny<Dictionary<string, object>>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyHeadersIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Headers = It.IsAny<Dictionary<string, object>>(), Times.Never);
        }

        [Test]
        public void ShouldCopyMessageId()
        {
            _sut.MessageId = Guid.NewGuid().ToString();

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.MessageId = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyMessageIdIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.MessageId = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void ShouldCopyPriority()
        {
            _sut.Priority = 0;

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Priority = It.IsAny<byte>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyPriorityIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Priority = It.IsAny<byte>(), Times.Never);
        }

        [Test]
        public void ShouldCopyReplyTo()
        {
            _sut.ReplyTo = "unique.routing.key";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ReplyTo = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyReplyToIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.ReplyTo = It.IsAny<string>(), Times.Never);
        }

        [Test]
        public void ShouldCopyType()
        {
            _sut.Type = "StarMQ.Model.Properties";

            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Type = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ShouldNotCopyTypeIfNotSet()
        {
            _sut.CopyTo(_basicProperties.Object);

            _basicProperties.VerifySet(x => x.Type = It.IsAny<string>(), Times.Never);
        }
        #endregion
    }
}