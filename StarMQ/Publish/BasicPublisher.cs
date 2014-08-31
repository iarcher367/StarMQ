namespace StarMQ.Publish
{
    using log4net;
    using RabbitMQ.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Offers no advanced functionality or messaging guarantees.
    /// </summary>
    public class BasicPublisher : BasePublisher
    {
        public BasicPublisher(ILog log) : base(log) { }

        public override Task Publish(IModel model, Action<IModel> action)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            try
            {
                SynchronizeModel(model);
                action(model);

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