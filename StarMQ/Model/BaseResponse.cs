#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Model
{
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

            log.Info(String.Format("Message #{0} processing succeeded - basic.ack sent.", DeliveryTag));
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