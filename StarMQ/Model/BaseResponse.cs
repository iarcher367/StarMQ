namespace StarMQ.Model
{
    using log4net;
    using RabbitMQ.Client;
    using System;

    public enum ResponseAction
    {
        DoNothing,
        Unsubscribe
    }

    public abstract class BaseResponse
    {
        public ResponseAction Action { get; set; }
        internal ulong DeliveryTag { get; set; }

        /// <summary>
        /// Set true to indicate up-to-and-including the message specified by the delivery tag.
        /// </summary>
        public bool Multiple { get; set; }

        public virtual void Send(IModel model, ILog log)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (log == null)
                throw new ArgumentNullException("log");
        }
    }

    public class AckResponse : BaseResponse
    {
        public override void Send(IModel model, ILog log)
        {
            base.Send(model, log);

            model.BasicAck(DeliveryTag, Multiple);

            log.Info(String.Format("Message #{0} successfully processed - basic.ack sent.", DeliveryTag));
        }
    }

    public class NackResponse : BaseResponse
    {
        /// <summary>
        /// Set true to instruct the server to attempt to requeue the message. If requeue fails
        /// or this is false, the message is dead-lettered (if configured) or discarded.
        /// </summary>
        public bool Requeue { get; set; }

        public override void Send(IModel model, ILog log)
        {
            base.Send(model, log);

            model.BasicNack(DeliveryTag, Multiple, Requeue);

            log.Info(String.Format("Message #{0} processing failed - basic.nack sent.", DeliveryTag));
        }
    }
}