namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public class TransientConsumer : BaseConsumer
    {
        public TransientConsumer(IConnectionConfiguration configuration, IConnection connection,
            IInboundDispatcher dispatcher, ILog log, INamingStrategy namingStrategy)
            : base(configuration, connection, dispatcher, log, namingStrategy)
        {
        }

        public override Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            if (messageHandler == null)
                throw new ArgumentNullException("messageHandler");

            MessageHandler = messageHandler;

            return Consume(queue);
        }

        protected Task Consume(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            ConsumerCancelled += (o, args) => Dispose();

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
                }
            }
            catch (Exception ex)    // TODO: research what kind of exceptions can be thrown
            {
                Log.Error(String.Format("Consumer '{0}' failed to consume queue '{1}'.", queue.Name, ConsumerTag), ex);
            }

            return tcs.Task;
        }
    }
}