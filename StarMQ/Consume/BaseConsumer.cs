namespace StarMQ.Consume
{
    using Core;
    using Exception;
    using log4net;
    using Message;
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public interface IConsumer : IBasicConsumer, IDisposable
    {
        Task Consume(Queue queue, IBasicConsumer consumer = null);
    }

    public abstract class BaseConsumer : IConsumer
    {
        protected readonly IConnectionConfiguration Configuration;
        protected readonly IConnection Connection;
        protected readonly IOutboundDispatcher Dispatcher;
        protected readonly ILog Log;

        private readonly IHandlerManager _handlerManager;
        private readonly IPipeline _pipeline;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ISerializationStrategy _serializationStrategy;
        private bool _disposed;

        public event ConsumerCancelledEventHandler ConsumerCancelled;

        public string ConsumerTag { get; private set; }
        public IModel Model { get; protected set; }

        protected BaseConsumer(IConnectionConfiguration configuration, IConnection connection,
            IOutboundDispatcher dispatcher, IHandlerManager handlerManager, ILog log,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy)
        {
            Configuration = configuration;
            Connection = connection;
            ConsumerTag = namingStrategy.GetConsumerTag();
            Dispatcher = dispatcher;
            _handlerManager = handlerManager;
            Log = log;
            _pipeline = pipeline;
            _serializationStrategy = serializationStrategy;

            Connection.OnDisconnected += OnDisconnected;
            Dispatch();
            Model = Connection.CreateModel();
        }

        private void Dispatch()
        {
            Task.Run(() =>
                {
                    foreach (var action in _queue.GetConsumingEnumerable())
                        Task.Run(action).Wait();
                });
        }

        private void OnDisconnected()
        {
            Action action;

            while (_queue.TryTake(out action))
                Log.Info("Message discarded.");
        }

        public abstract Task Consume(Queue queue, IBasicConsumer consumer = null);

        public virtual void HandleBasicCancel(string consumerTag)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            var consumerCancelled = ConsumerCancelled;
            if (consumerCancelled != null)
                consumerCancelled(this, new ConsumerEventArgs(consumerTag));

            Log.Info(String.Format("Broker cancelled consumer '{0}'.", consumerTag));
        }

        public void HandleBasicCancelOk(string consumerTag)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            Log.Info(String.Format("Cancel confirmed for consumer '{0}'.", consumerTag));
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

            Log.Debug(String.Format("Consumer '{0}' received message #{1}.", consumerTag, deliveryTag));

            try
            {
                _queue.Add(() => // TODO: process redelivered?
                    {
                        var message = new Message<byte[]>(body);
                        message.Properties.CopyFrom(properties);

                        if (_disposed) return;

                        var data = _pipeline.OnReceive(message);
                        var processed = _serializationStrategy.Deserialize(data, _handlerManager.Default);
                        var handler = _handlerManager.Get(processed.Body.GetType());
                        var response = (BaseResponse)handler(processed.Body);
                        response.DeliveryTag = deliveryTag;

                        try
                        {
                            response.Send(Model, Log);

                            if (response.Action == ResponseAction.Unsubscribe)
                            {
                                Model.BasicCancel(ConsumerTag);
                                Dispose();
                            }
                        }
                        catch (AlreadyClosedException ex)
                        {
                            Log.Info(String.Format("Unable to send response. Lost connection to broker - {0}.",
                                ex.GetType().Name));
                        }
                        catch (IOException ex)
                        {
                            Log.Info(String.Format("Unable to send response. Lost connection to broker - {0}.",
                                ex.GetType().Name));
                        }
                        catch (NotSupportedException ex)
                        {
                            Log.Info(String.Format("Unable to send response. Lost connection to broker - {0}.",
                                ex.GetType().Name));
                        }
                    });
            }
            catch (InvalidOperationException) { /* thrown if fired after dispose */}
        }

        public void HandleModelShutdown(IModel model, ShutdownEventArgs args)
        {
            Log.Info(String.Format("Consumer '{0}' shutdown by {1}. Reason: '{2}'",
                ConsumerTag, args.Initiator, args.Cause));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _queue.CompleteAdding();
            OnDisconnected();

            Model.Dispose();

            Log.Info("Dispose completed.");
        }
    }
}