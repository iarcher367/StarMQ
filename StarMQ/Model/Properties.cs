namespace StarMQ.Model
{
    using Exception;
    using RabbitMQ.Client;
    using System;
    using System.Collections.Generic;

    public class Properties
    {
        private string _contentEncoding;
        private string _contentType;
        private string _correlationId;
        private byte _deliveryMode;
        private IDictionary<string, object> _headers;
        private string _messageId;
        private byte _priority;
        private string _replyTo;
        private string _type;

        private bool _contentEncodingPresent;
        private bool _contentTypePresent;
        private bool _correlationIdPresent;
        private bool _deliveryModePresent;
        private bool _headersPresent;
        private bool _messageIdPresent;
        private bool _typePresent;
        private bool _replyToPresent;
        private bool _priorityPresent;

        /// <summary>
        /// MIME encoding
        /// </summary>
        public string ContentEncoding
        {
            get { return _contentEncoding; }
            set { _contentEncoding = Global.Validate("ContentEncoding", value); _contentEncodingPresent = true; }
        }

        /// <summary>
        /// MIME type
        /// </summary>
        public string ContentType
        {
            get { return _contentType; }
            set { _contentType = Global.Validate("ContentType", value); _contentTypePresent = true; }
        }

        /// <summary>
        /// Application correlation identifier
        /// </summary>
        public string CorrelationId
        {
            get { return _correlationId; }
            set { _correlationId = Global.Validate("CorrelationId", value); _correlationIdPresent = true; }
        }

        /// <summary>
        /// Transient (1) or Persistent (2)
        /// </summary>
        public byte DeliveryMode
        {
            get { return _deliveryMode; }
            set
            {
                if (value != 1 && value != 2)
                    throw new InvalidValueException("DeliveryMode", value.ToString());

                _deliveryMode = value;
                _deliveryModePresent = true;
            }
        }

        public IDictionary<string, object> Headers
        {
            get { return _headers; }
            set { _headers = value; _headersPresent = true; }
        }

        /// <summary>
        /// Application message identifier
        /// </summary>
        public string MessageId
        {
            get { return _messageId; }
            set { _messageId = Global.Validate("MessageId", value); _messageIdPresent = true; }
        }

        /// <summary>
        /// Priority from 0 to 9
        /// </summary>
        public byte Priority
        {
            get { return _priority; }
            set
            {
                if (value > 9)
                    throw new InvalidValueException("Priority", value.ToString());

                _priority = value;
                _priorityPresent = true;
            }
        }

        /// <summary>
        /// Response destination for RPC calls
        /// </summary>
        public string ReplyTo
        {
            get { return _replyTo; }
            set { _replyTo = Global.Validate("ReplyTo", value); _replyToPresent = true; }
        }

        /// <summary>
        /// C# type
        /// </summary>
        public string Type
        {
            get { return _type; }
            set { _type = Global.Validate("Type", value); _typePresent = true; }
        }

        public Properties()
        {
            _headers = new Dictionary<string, object>();
        }

        public void CopyTo(IBasicProperties target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (_contentEncodingPresent) target.ContentEncoding = ContentEncoding;
            if (_contentTypePresent) target.ContentType = ContentType;
            if (_correlationIdPresent) target.CorrelationId = CorrelationId;
            if (_deliveryModePresent) target.DeliveryMode = DeliveryMode;
            if (_headersPresent) target.Headers = Headers;
            if (_messageIdPresent) target.MessageId = MessageId;
            if (_priorityPresent) target.Priority = Priority;
            if (_replyToPresent) target.ReplyTo = ReplyTo;
            if (_typePresent) target.Type = Type;
        }
    }
}