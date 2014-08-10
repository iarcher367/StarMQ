namespace StarMQ.Publish
{
    using log4net;
    using RabbitMQ.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Offers no advanced functionality.
    /// </summary>
    public class BasicPublisher : BasePublisher
    {
        public BasicPublisher(ILog log) : base(log) { }

        public override Task Publish(IModel model, Action<IModel> action)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                SynchronizeModel(model);
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