namespace StarMQ.Consume
{
    using Core;
    using log4net;
    using Model;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// This consumer is designed for exclusive or self-destructing queues as it does not
    /// re-subscribe to the specified queue after the system recovers the connection to the broker.
    /// </summary>
    public class BasicConsumer : BaseConsumer
    {
        public BasicConsumer(IConnectionConfiguration configuration, IConnection connection,
            IOutboundDispatcher dispatcher, ILog log, INamingStrategy namingStrategy)
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

        protected async Task Consume(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");

            await Dispatcher.Invoke(() =>
            {
                var args = new Dictionary<string, object>();

                Model.BasicQos(0, Configuration.PrefetchCount, false);

                if (Configuration.CancelOnHaFailover || queue.CancelOnHaFailover)
                    args.Add("x-cancel-on-ha-failover", queue.CancelOnHaFailover);
                if (queue.Priority != 0)
                    args.Add("x-priority", queue.Priority);

                Model.BasicConsume(queue.Name, false, ConsumerTag, args, this);

                Log.Info(String.Format("Consumer '{0}' declared on queue '{1}'.", ConsumerTag, queue.Name));
            });
        }
    }
}