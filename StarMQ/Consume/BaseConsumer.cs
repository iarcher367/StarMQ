namespace StarMQ.Consume
{
    using Core;
    using Exception;
    using log4net;
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// To ensure sequential message processing, all consumers created by the same bus share a single
    /// dispatcher thread. Avoid using long-running message handlers for high throughput scenarios.
    /// </summary>
    public interface IConsumer : IBasicConsumer, IDisposable
    {
        Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler);
    }

    public abstract class BaseConsumer : IConsumer
    {
        protected readonly IConnectionConfiguration Configuration;
        protected readonly IConnection Connection;
        protected readonly ILog Log;
        protected Func<IMessage<byte[]>, BaseResponse> MessageHandler;

        private readonly IInboundDispatcher _dispatcher;
        private bool _disposed;

        public event ConsumerCancelledEventHandler ConsumerCancelled;

        public string ConsumerTag { get; private set; }
        public IModel Model { get; protected set; }

        protected BaseConsumer(IConnectionConfiguration configuration, IConnection connection,
            IInboundDispatcher dispatcher, ILog log, INamingStrategy namingStrategy)
        {
            Configuration = configuration;
            Connection = connection;
            ConsumerTag = namingStrategy.GetConsumerTag();
            _dispatcher = dispatcher;
            Log = log;
            Model = connection.CreateModel();
        }

        public abstract Task Consume(Queue queue, Func<IMessage<byte[]>, BaseResponse> messageHandler);

        public void HandleBasicCancel(string consumerTag)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            var consumerCancelled = ConsumerCancelled;
            if (consumerCancelled != null)
                consumerCancelled(this, new ConsumerEventArgs(consumerTag));

            Log.Info(String.Format("Cancel requested by broker for consumer '{0}'.", consumerTag));
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            Log.Warn(String.Format("Cancel confirmed for consumer '{0}' - research required!", consumerTag));

            Dispose();
        }

        public void HandleBasicConsumeOk(string consumerTag)
        {
            if (consumerTag == null)
                throw new ArgumentNullException("consumerTag");

            ConsumerTag = consumerTag;

            Log.Info(String.Format("Consume confirmed for consumer '{0}'.", consumerTag));
        }

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered,
            string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            Log.Debug(String.Format("Consumer '{0}' received message with deliveryTag '{1}'.",
                consumerTag, deliveryTag));

            // TODO: check _disposed?

            if (MessageHandler == null)
                Log.Warn(String.Format("Message handler has not been set for consumer '{0}'", consumerTag));
            else
            {
                var message = new Message<byte[]>(body);
                message.Properties.CopyFrom(properties);

                _dispatcher.Invoke(async () =>       // TODO: may need to pass in redelivered
                    {
                        var response = MessageHandler(message);
                        response.DeliveryTag = deliveryTag;

                        try
                        {
                            await SendResponse(response);
                        }
                        catch (AlreadyClosedException ex)
                        {
                            Log.Info(String.Format("Lost connection to broker - {0}.", ex.GetType().Name));
                        }
                        catch (NotSupportedException ex)
                        {
                            Log.Info(String.Format("Lost connection to broker - {0}.", ex.GetType().Name));
                        }
                    });
            }
        }

        private Task SendResponse(BaseResponse response)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                response.Send(Model, Log);

                if (response.Action == ResponseAction.Unsubscribe)
                    Model.BasicCancel(ConsumerTag);

                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs args)
        {
            Log.Info(String.Format("Consumer '{0}' was shutdown by '{1}' due to '{2}'",
                ConsumerTag, args.Initiator, args.Cause));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            Model.Dispose();

            Log.Info("Dispose completed.");
        }
    }
}