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

namespace StarMQ.Publish
{
    using Core;
    using Exception;
    using Model;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// Guarantees messaging via publisher confirms and ensures order of unconfirmed messages.
    /// </summary>
    internal class ConfirmPublisherDecorator : IPublisher
    {
        private readonly static Object LockObj = new Object();
        private readonly IConnectionConfiguration _configuration;
        private readonly IOutboundDispatcher _dispatcher;
        private readonly ILog _log;
        private readonly IPublisher _publisher;
        private readonly ConcurrentDictionary<ulong, Context> _unconfirmedMessages = new ConcurrentDictionary<ulong, Context>();

        public IModel Model { get { return _publisher.Model; } }

        public event BasicReturnHandler BasicReturn;

        private class Context
        {
            private readonly List<ulong> _sequenceIds = new List<ulong>();

            public Action Action { get; set; }
            public List<ulong> SequenceIds { get { return _sequenceIds; } }
            public TaskCompletionSource<object> Source { get; set; }
            public Timer Timer { get; set; }
        }

        public ConfirmPublisherDecorator(IPublisher publisher, IConnectionConfiguration configuration,
            IConnection connection, IOutboundDispatcher dispatcher, ILog log)
        {
            _configuration = configuration;
            _dispatcher = dispatcher;
            _log = log;
            _publisher = publisher;

            OnConnected();

            connection.OnConnected += OnConnected;
            connection.OnDisconnected += OnDisconnected;

            _publisher.BasicReturn += (o, e) =>
            {
                var basicReturn = BasicReturn;
                if (basicReturn != null)
                    basicReturn(o, e);
            };
        }

        protected void OnConnected()
        {
            Model.BasicAcks += ModelOnBasicAcks;
            Model.BasicNacks += ModelOnBasicNacks;
            Model.ConfirmSelect();

            RequeueUnconfirmedMessages();
        }

        protected void OnDisconnected()
        {
            Model.BasicAcks -= ModelOnBasicAcks;
            Model.BasicNacks -= ModelOnBasicNacks;

            _unconfirmedMessages.Values.ToList().ForEach(x =>
            {
                if (x.Timer == null) return;

                x.Timer.Dispose();
                _log.Info(String.Format("Message #{0} timer disposed.", x.SequenceIds[0]));
            });
        }

        private void RequeueUnconfirmedMessages()
        {
            var cached = new ConcurrentDictionary<ulong, Context>(_unconfirmedMessages);
            _unconfirmedMessages.Clear();

            foreach (var key in cached.Keys.OrderBy(x => x).Where(x => x == cached[x].SequenceIds.First()))
            {
                var context = new Context
                {
                    Action = cached[key].Action,
                    Source = cached[key].Source,
                };
                _dispatcher.Invoke(() => Publish(context));
            }
        }

        private void Publish(Context context)
        {
            DisposeTimer(context.Timer);

            ulong sequenceId;

            lock (LockObj)
            {
                sequenceId = Model.NextPublishSeqNo;
                context.Action();
            }

            if (!_unconfirmedMessages.TryAdd(sequenceId, context))
                _log.Warn(String.Format("Duplicate sequenceId - failed to add #{0} to dictionary.", sequenceId));

            context.SequenceIds.Add(sequenceId);
            context.Timer = CreateTimer(sequenceId);

            if (context.SequenceIds.Count == 1)
                _log.Info(String.Format("Message #{0} published.", sequenceId));
            else
                _log.Info(String.Format("Message #{0} republished as #{1}.",
                    context.SequenceIds.OrderBy(x => x).First(), sequenceId));
        }

        private Timer CreateTimer(ulong sequenceId)
        {
            return new Timer(x =>
            {
                _log.Warn(String.Format("Message #{0} timed out waiting for broker response.",
                    sequenceId));

                _dispatcher.Invoke(() =>
                {
                    Context context;
                    if (_unconfirmedMessages.TryGetValue(sequenceId, out context))
                        Publish(context);
                });
            }, null, new TimeSpan(0, 0, 0, 0, _configuration.Timeout), Timeout.InfiniteTimeSpan);
        }

        private static void DisposeTimer(Timer timer)
        {
            if (timer != null)
                timer.Dispose();
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            ProcessResponse(args.DeliveryTag, args.Multiple, x =>
            {
                _log.Info(String.Format("Message #{0} confirmed. Multiple: {1}",
                    args.DeliveryTag, args.Multiple));

                x.Source.TrySetResult(null);
            });
        }

        /// <summary>
        /// Received upon internal broker error; unable to queue message.
        /// </summary>
        private void ModelOnBasicNacks(IModel model, BasicNackEventArgs args)
        {
            ProcessResponse(args.DeliveryTag, args.Multiple, x =>
            {
                _log.Error(String.Format("Internal broker error for message #{0}. Multiple: {1}",
                    args.DeliveryTag, args.Multiple));

                x.Source.TrySetException(new PublishException());
            });
        }

        private void ProcessResponse(ulong sequenceId, bool multiple, Action<Context> action)
        {
            var func = multiple
                ? (Func<ulong, bool>)(key => key <= sequenceId)
                : key => key == sequenceId;

            foreach (var id in _unconfirmedMessages.Keys.Where(func))
            {
                Context context;
                if (_unconfirmedMessages.TryRemove(id, out context))
                {
                    DisposeTimer(context.Timer);
                    action(context);
                    RemoveKeys(context.SequenceIds.Skip(1));
                }
            }
        }

        private void RemoveKeys(IEnumerable<ulong> keys)
        {
            Context tmp;
            foreach (var key in keys)
                _unconfirmedMessages.TryRemove(key, out tmp);
        }

        public Task Publish<T>(IMessage<T> message, Action<IModel, IBasicProperties, byte[]> action) where T : class
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            Publish(new Context
            {
                Action = () => _publisher.Publish(message, action),
                Source = tcs
            });

            return tcs.Task;
        }

        public void Dispose()
        {
            _publisher.Dispose();
        }
    }
}