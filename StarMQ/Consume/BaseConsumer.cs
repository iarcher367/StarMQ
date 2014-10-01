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

namespace StarMQ.Consume
{
    using Core;
    using Exception;
    using Message;
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public interface IConsumer : IBasicConsumer, IDisposable
    {
        Task Consume(Queue queue, Action<IHandlerRegistrar> configure = null, IBasicConsumer consumer = null);
    }

    internal abstract class BaseConsumer : IConsumer
    {
        protected readonly IConnection Connection;
        protected readonly IHandlerManager HandlerManager;
        protected readonly ILog Log;

        private readonly IPipeline _pipeline;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ManualResetEvent _signal = new ManualResetEvent(true);
        private readonly ISerializationStrategy _serializationStrategy;
        private bool _disposed;

        public event ConsumerCancelledEventHandler ConsumerCancelled;

        public string ConsumerTag { get; private set; }
        public IModel Model { get; protected set; }

        protected BaseConsumer(IConnection connection, IHandlerManager handlerManager, ILog log,
            INamingStrategy namingStrategy, IPipeline pipeline, ISerializationStrategy serializationStrategy)
        {
            Connection = connection;
            ConsumerTag = namingStrategy.GetConsumerTag();
            HandlerManager = handlerManager;
            Log = log;
            _pipeline = pipeline;
            _serializationStrategy = serializationStrategy;

            Connection.OnDisconnected += ClearQueue;
            Dispatch();
        }

        private void Dispatch()
        {
            Task.Run(() =>
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                {
                    Task.Run(action).Wait();

                    _signal.WaitOne(-1);
                }
            });
        }

        private void ClearQueue()
        {
            Action action;

            _signal.Reset();

            while (_queue.TryTake(out action))
                Log.Info("Message discarded.");

            _signal.Set();
        }

        public abstract Task Consume(Queue queue, Action<IHandlerRegistrar> configure = null, IBasicConsumer consumer = null);

        public virtual void HandleBasicCancel(string consumerTag)
        {
            if (ConsumerTag != consumerTag)
                throw new StarMqException("Consumer tag mismatch.");    // TODO: remove if impossible

            ClearQueue();

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
                    BaseResponse response;
                    var message = new Message<byte[]>(body);
                    message.Properties.CopyFrom(properties);

                    if (_disposed) return;

                    try
                    {
                        var data = _pipeline.OnReceive(message);
                        var processed = _serializationStrategy.Deserialize(data, HandlerManager.Default);
                        var handler = HandlerManager.Get(processed.Body.GetType());
                        response = (BaseResponse)handler(processed.Body);
                        response.DeliveryTag = deliveryTag;
                    }
                    catch (Exception ex)
                    {
                        response = new NackResponse { DeliveryTag = deliveryTag };
                        Log.Error(String.Format("Failed to process message #{0}", deliveryTag),
                            ex);
                    }

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
            ClearQueue();

            if (Model != null)
                Model.Dispose();

            Log.Info("Dispose completed.");
        }
    }
}