namespace StarMQ.Publish
{
    using Core;
    using Exception;
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// Guarantees messaging via publisher confirms.
    /// </summary>
    public sealed class ConfirmPublisher : BasePublisher
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly ConcurrentDictionary<ulong, PublishState> _pendingMessages = new ConcurrentDictionary<ulong, PublishState>();

        public ConfirmPublisher(IConnectionConfiguration configuration, IConnection connection,
            ILog log) : base(connection, log)
        {
            _configuration = configuration;

            OnConnected();
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            Model.BasicAcks -= ModelOnBasicAcks;
            Model.BasicNacks -= ModelOnBasicNacks;
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            Model.BasicAcks += ModelOnBasicAcks;
            Model.BasicNacks += ModelOnBasicNacks;
            Model.ConfirmSelect();

            RequeuePendingMessages();
        }

        private void RequeuePendingMessages()
        {
            var requeued = new ConcurrentDictionary<ulong, PublishState>(_pendingMessages);
            _pendingMessages.Clear();

            requeued.Values.ToList().ForEach(x => x.Timer.Dispose());

            foreach (var state in requeued.Values)
            {
                var sequenceId = Model.NextPublishSeqNo;

                state.Action(Model);

                _pendingMessages.TryAdd(sequenceId, new PublishState
                    {
                        Action = state.Action,
                        Source = state.Source,
                        Timer = CreateTimer(sequenceId, state.Source)
                    });
            }
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            ProcessResponse(args.DeliveryTag, args.Multiple, x =>
            {
                Log.Info(String.Format("Message #{0} confirmed. Multiple: {1}",
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
                Log.Error(String.Format("Internal broker error for message #{0}. Multiple: {1}",
                    args.DeliveryTag, args.Multiple));

                x.Timer.Dispose();
                x.Source.TrySetException(new PublishException());
            });
        }

        private void ProcessResponse(ulong sequenceId, bool multiple, Action<PublishState> action)
        {
            PublishState tmp;

            if (multiple)
            {
                foreach (var id in _pendingMessages.Keys.Where(key => key <= sequenceId))
                {
                    action(_pendingMessages[id]);
                    _pendingMessages.TryRemove(id, out tmp);
                }
            }
            else
            {
                if (_pendingMessages.ContainsKey(sequenceId))
                {
                    action(_pendingMessages[sequenceId]);
                    _pendingMessages.TryRemove(sequenceId, out tmp);
                }
            }
        }

        public override Task Publish(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            var sequenceId = Model.NextPublishSeqNo;

            _pendingMessages.TryAdd(sequenceId, new PublishState
            {
                Action = action,
                Source = tcs,
                Timer = CreateTimer(sequenceId, tcs)
            });

            action(Model);

            return tcs.Task;
        }

        private Timer CreateTimer(ulong sequenceId, TaskCompletionSource<object> tcs)
        {
            return new Timer(x =>
            {
                var text = String.Format("Publish sequence #{0} timed out waiting for broker response.", sequenceId);

                Log.Warn(text);

                if (tcs.TrySetException(new TimeoutException(text)))
                {
                    PublishState tmp;
                    var message = _pendingMessages[sequenceId];

                    message.Timer.Dispose();
                    _pendingMessages.TryRemove(sequenceId, out tmp);
                }
            }, null, _configuration.Timeout * 1000, Timeout.Infinite);
        }

        private class PublishState
        {
            public Action<IModel> Action { get; set; }
            public TaskCompletionSource<object> Source { get; set; }
            public Timer Timer { get; set; }
        }
    }
}