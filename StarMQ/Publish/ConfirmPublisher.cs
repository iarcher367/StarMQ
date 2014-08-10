namespace StarMQ.Publish
{
    using Core;
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Guarantees messaging via publisher confirms.
    /// </summary>
    public class ConfirmPublisher : BasePublisher
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IDictionary<ulong, object> _pendingMessages = new Dictionary<ulong, object>();

        public ConfirmPublisher(IConnectionConfiguration configuration, ILog log) : base(log)
        {
            _configuration = configuration;
        }

        protected override void OnChannelClosed(IModel model)
        {
            model.BasicAcks -= ModelOnBasicAcks;
            model.BasicNacks -= ModelOnBasicNacks;

            _pendingMessages.Clear();

            base.OnChannelClosed(model);
        }

        protected override void OnChannelOpened(IModel model)
        {
            model.BasicAcks += ModelOnBasicAcks;
            model.BasicNacks += ModelOnBasicNacks;

            model.ConfirmSelect();

            base.OnChannelOpened(model);
        }

        private void ModelOnBasicNacks(IModel model, BasicNackEventArgs args)
        {
            Log.Debug("Basic.Nack received.");

            ProcessResponse(args.DeliveryTag, args.Multiple, null);
        }

        private void ModelOnBasicAcks(IModel model, BasicAckEventArgs args)
        {
            Log.Debug("Basic.Ack received.");

            ProcessResponse(args.DeliveryTag, args.Multiple, null);
        }

        // TODO: test this extensively
        private void ProcessResponse(ulong sequenceId, bool multiple, Action<object> action)
        {
            if (multiple)
            {
                foreach (var id in _pendingMessages.Keys.Where(key => key <= sequenceId))
                {
                    action(_pendingMessages[id]);
                    _pendingMessages.Remove(id);
                }
            }
            else
            {
                if (_pendingMessages.ContainsKey(sequenceId))
                {
                    action(_pendingMessages[sequenceId]);
                    _pendingMessages.Remove(sequenceId);
                }
            }
        }

        public override Task Publish(IModel model, Action<IModel> action)
        {
//            throw new NotImplementedException();

            var tcs = new TaskCompletionSource<object>();   // TODO: wip

            try
            {
                var sequenceId = model.NextPublishSeqNo;

                Timer timer = null;
                timer = new Timer(state =>
                                  {
                                      Log.Warn(String.Format("Publish sequence #{0} timed out.", sequenceId));

                                      _pendingMessages.Remove(sequenceId);
                                      timer.Dispose();
                                  }, null, _configuration.Timeout * 1000, Timeout.Infinite);

                action(model);

                Log.Info("Published message.");

                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }
}