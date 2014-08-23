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

            Log.Info("Synchronizing model.");

            _cachedModel = model;

            OnChannelOpened(model);
        }

        protected virtual void OnChannelClosed(IModel model)
        {
            model.BasicReturn -= ModelOnBasicReturn;
        }

        protected virtual void OnChannelOpened(IModel model)
        {
            model.BasicReturn += ModelOnBasicReturn;
        }

        protected void ModelOnBasicReturn(IModel model, BasicReturnEventArgs args)
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
