namespace StarMQ.Publish
{
    using Core;
    using log4net;
    using RabbitMQ.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Use this when publisher confirms and transactions are not required.
    /// </summary>
    public class BasicPublisher : PublisherBase
    {
        public BasicPublisher(ILog log) : base(log) { }

        public override Task Publish(IModel model, Action<IModel> action)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                SetModel(model);
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

    /// <summary>
    /// Use this for publisher confirms.
    /// </summary>
    public class ConfirmPublisher : PublisherBase
    {
        private readonly IConnectionConfiguration _configuration;

        public ConfirmPublisher(IConnectionConfiguration configuration, ILog log) : base(log)
        {
            _configuration = configuration;
        }

        protected override void OnChannelOpened(IModel newModel)
        {
            newModel.ConfirmSelect();

            // subscribe BasicAcks, BasicNacks

            base.OnChannelOpened(newModel);
        }

        protected override void OnChannelClosed(IModel oldModel)
        {
            throw new NotImplementedException();
        }

        public override Task Publish(IModel model, Action<IModel> action)
        {
            throw new NotImplementedException();
        }
    }
}