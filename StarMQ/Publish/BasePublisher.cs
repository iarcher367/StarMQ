namespace StarMQ.Publish
{
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Threading.Tasks;

    public interface IPublisher
    {
        Task Publish(IModel model, Action<IModel> action);
    }

    /// <summary>
    /// All publishes are done over a single channel and on a single thread to enforce clear ownership
    /// of thread-unsafe IModel instances; see RabbitMQ .NET client documentation section 2.10. 
    /// </summary>
    public abstract class BasePublisher : IPublisher
    {
        protected readonly ILog Log;

        private IModel _cachedModel;

        protected BasePublisher(ILog log)
        {
            Log = log;
        }

        /// <summary>
        /// Synchronizes cached model to capture changes from persistent channel
        /// </summary>
        protected void SynchronizeModel(IModel model)
        {
            if (_cachedModel == model) return;

            if (_cachedModel != null)
                OnChannelClosed(_cachedModel);

            _cachedModel = model;

            OnChannelOpened(model);

            Log.Info("Synchronized model.");
        }

        protected virtual void OnChannelClosed(IModel model)
        {
            model.BasicReturn -= HandleBasicReturn;
        }

        protected virtual void OnChannelOpened(IModel model)
        {
            model.BasicReturn += HandleBasicReturn;
        }

        private void HandleBasicReturn(IModel model, BasicReturnEventArgs args)
        {
            const string format = "Basic.Return received for message with correlationId '{0}' " +
                                  "from exchange '{1}' with code '{2}:{3}'";

            Log.Warn(String.Format(format, args.BasicProperties.CorrelationId, args.Exchange,
                args.ReplyCode, args.ReplyText));

            throw new NotImplementedException();    // TODO: basic.return should fire an event to calling code
        }

        public abstract Task Publish(IModel model, Action<IModel> action);
    }
}
