namespace StarMQ.Consume
{
    using System.IO;
    using Core;
    using Exception;
    using log4net;
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
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
        protected readonly IConsumerDispatcher Dispatcher;
        protected readonly ILog Log;
        protected bool Disposed;
        protected Func<IMessage<byte[]>, BaseResponse> MessageHandler;

        public event ConsumerCancelledEventHandler ConsumerCancelled;
        public string ConsumerTag { get; private set; }
        public IModel Model { get; private set; }

        protected BaseConsumer(IConnection connection, IConsumerDispatcher dispatcher, ILog log,
            INamingStrategy namingStrategy)
        {
            ConsumerTag = namingStrategy.GetConsumerTag();
            Dispatcher = dispatcher;
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

                Dispatcher.Invoke(async () =>       // TODO: may need to pass in redelivered
                    {
                        try
                        {
                            var response = MessageHandler(message);
                            response.DeliveryTag = deliveryTag;

                            await SendResponse(response);
                        }
                        catch (IOException)
                        {
                            Log.Info("Lost connection to broker.");
                        }
                        catch (Exception)
                        {
                            SendResponse(new NackResponse { DeliveryTag = deliveryTag }).Wait();
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
                {
                    Model.BasicCancel(ConsumerTag);
                    Dispose();
                }

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
            Log.Info(String.Format("Consumer '{0}' shutdown by '{1}' due to '{2}'",
                ConsumerTag, args.Initiator, args.Cause));

            Dispose();
        }

        public void Dispose()
        {
            if (Disposed) return;

            Disposed = true;

            Dispatcher.Dispose();
            Model.Dispose();

            Log.Info("Disposal complete.");
        }
    }
}