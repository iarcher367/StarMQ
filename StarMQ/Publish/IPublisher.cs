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

    public abstract class PublisherBase : IPublisher
    {
        protected readonly ILog Log;
        private IModel _cachedModel;

        protected PublisherBase(ILog log)
        {
            Log = log;
        }

        /// <summary>
        /// Synchronizes cached model to capture changes from persistent channel
        /// </summary>
        protected void SetModel(IModel model)
        {
            if (_cachedModel == model) return;

            if (_cachedModel != null)
                OnChannelClosed(_cachedModel);

            Log.Info("Synchronizing model.");

            _cachedModel = model;

            OnChannelOpened(model);
        }

        protected virtual void OnChannelClosed(IModel oldModel)
        {
            oldModel.BasicReturn -= ModelOnBasicReturn;
        }

        protected virtual void OnChannelOpened(IModel newModel)
        {
            newModel.BasicReturn += ModelOnBasicReturn;
        }

        protected void ModelOnBasicReturn(IModel model, BasicReturnEventArgs args)
        {
            throw new NotImplementedException();    // TODO: handle basic.return'd messages
        }

        public abstract Task Publish(IModel model, Action<IModel> action);
    }
}
