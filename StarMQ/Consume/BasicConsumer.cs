namespace StarMQ.Consume
{
    using Core;
    using Exception;
    using log4net;
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// This consumer is designed for exclusive or self-destructing queues as it does not re-subscribe
    /// to the specified queue after the system recovers the connection to the broker.
    /// </summary>
    public class BasicConsumer : BaseConsumer
    {
        public BasicConsumer(IConnectionConfiguration configuration, IConnection connection,
            IInboundDispatcher dispatcher, ILog log, INamingStrategy namingStrategy)
            : base(configuration, connection, dispatcher, log, namingStrategy)
        {
        }

        public override async Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler)
        {
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            MessageHandler = messageHandler;

            await Consume(queue);   // TODO: catch exceptions
        }

        protected Task Consume(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            var tcs = new TaskCompletionSource<object>();

            try
            {
                if (Model.IsOpen)
                {
                    var args = new Dictionary<string, object>();

                    Model.BasicQos(0, Configuration.PrefetchCount, false);

                    // TODO: add args - priority, cancel

                    Model.BasicConsume(queue.Name, false, ConsumerTag, args, this);

                    Log.Info(String.Format("Consumer '{0}' declared on queue '{1}'.", ConsumerTag, queue.Name));

                    tcs.SetResult(null);
                }
                else
                {
                    Log.Warn(String.Format("Consumer '{0}' cannot consume queue '{1}' - channel closed.", ConsumerTag, queue.Name));

//                    tcs.SetException(new NackedPublishException("consume"));    // TODO: verify
                }
            }
            catch (Exception ex)
            {
                Log.Error(String.Format("Consumer '{0}' failed to consume queue '{1}'.", ConsumerTag, queue.Name), ex);

                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }
}