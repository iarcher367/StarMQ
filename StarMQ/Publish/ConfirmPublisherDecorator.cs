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
    using log4net;
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
    public class ConfirmPublisherDecorator : IPublisher
    {
        private readonly static Object LockObj = new Object();
        private readonly ConcurrentDictionary<ulong, PublishState> _pendingMessages = new ConcurrentDictionary<ulong, PublishState>();
        private readonly IConnection _connection;
        private readonly ILog _log;
        private readonly IPublisher _publisher;
        private readonly IConnectionConfiguration _configuration;

        public IModel Model { get { return _publisher.Model; } }

        public event BasicReturnHandler BasicReturn;

        private class PublishState
        {
            private readonly List<ulong> _sequenceIds = new List<ulong>();

            public List<ulong> SequenceIds { get { return _sequenceIds; } }
            public Action Action { get; set; }
            public TaskCompletionSource<object> Source { get; set; }
            public Timer Timer { get; set; }
        }

        public ConfirmPublisherDecorator(IPublisher publisher, IConnectionConfiguration configuration,
            IConnection connection, ILog log)
        {
            _connection = connection;
            _log = log;
            _publisher = publisher;
            _configuration = configuration;

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

            RequeuePendingMessages();
        }

        protected void OnDisconnected()
        {
            Model.BasicAcks -= ModelOnBasicAcks;
            Model.BasicNacks -= ModelOnBasicNacks;

            _pendingMessages.Values.ToList().ForEach(x =>
            {
                if (x.Timer != null)
                    x.Timer.Dispose();

                _log.Info("Timer disposed.");
            });
        }

        private void RequeuePendingMessages()
        {
            var requeued = new ConcurrentDictionary<ulong, PublishState>(_pendingMessages);
            _pendingMessages.Clear();

            foreach (var key in requeued.Keys.OrderBy(x => x)
                                .Where(x => x == requeued[x].SequenceIds.OrderBy(y => y).First()))
                Publish(new PublishState
                {
                    Action = requeued[key].Action,
                    Source = requeued[key].Source
                });
        }

        private void Publish(PublishState state)
        {
            if (state.Timer != null)
                state.Timer.Dispose();

            ulong sequenceId;

            lock (LockObj)
            {
                sequenceId = Model.NextPublishSeqNo;
                state.Action();
            }

            _pendingMessages.TryAdd(sequenceId, state);

            state.SequenceIds.Add(sequenceId);
            state.Timer = CreateTimer(sequenceId);

            if (state.SequenceIds.Count == 1)
                _log.Info(String.Format("Message #{0} published.", sequenceId));
            else
                _log.Info(String.Format("Message #{0} republished as #{1}.",
                    state.SequenceIds.OrderBy(x => x).First(), sequenceId));
        }

        private Timer CreateTimer(ulong sequenceId)
        {
            return new Timer(x =>
            {
                _log.Warn(String.Format("Message #{0} timed out waiting for broker response.",
                    sequenceId));

                if (_connection.IsConnected)
                {
                    try
                    {
                        Publish(_pendingMessages[sequenceId]);
                    }
                    catch (Exception ex)    // TODO: occurs if timeout < heartbeat; refactor pending
                    {
                        _log.Warn("Unable to publish timed out message.", ex);
                    }
                }
            }, null, new TimeSpan(0, 0, 0, 0, _configuration.Timeout), Timeout.InfiniteTimeSpan);
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            ProcessResponse(args.DeliveryTag, args.Multiple, x =>
            {
                _log.Info(String.Format("Message #{0} confirmed. Multiple: {1}",
                    args.DeliveryTag, args.Multiple));

                x.Timer.Dispose();
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

                x.Timer.Dispose();
                x.Source.TrySetException(new PublishException());
            });
        }

        private void ProcessResponse(ulong sequenceId, bool multiple, Action<PublishState> action)
        {
            if (multiple)
            {
                foreach (var id in _pendingMessages.Keys.Where(key => key <= sequenceId))
                {
                    action(_pendingMessages[id]);
                    RemoveAllRelatedKeys(id);
                }
            }
            else
            {
                if (_pendingMessages.ContainsKey(sequenceId))
                {
                    action(_pendingMessages[sequenceId]);
                    RemoveAllRelatedKeys(sequenceId);
                }
            }
        }

        private void RemoveAllRelatedKeys(ulong sequenceId)
        {
            PublishState tmp;

            foreach (var key in _pendingMessages[sequenceId].SequenceIds)
                _pendingMessages.TryRemove(key, out tmp);
        }

        public Task Publish<T>(IMessage<T> message, Action<IModel, IBasicProperties, byte[]> action) where T : class
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            Publish(new PublishState
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