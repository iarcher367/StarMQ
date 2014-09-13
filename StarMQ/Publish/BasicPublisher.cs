namespace StarMQ.Publish
{
    using log4net;
    using RabbitMQ.Client;
    using System;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    /// <summary>
    /// Offers no advanced functionality or messaging guarantees.
    /// </summary>
    public sealed class BasicPublisher : BasePublisher
    {
        public BasicPublisher(IConnection connection, ILog log) : base(connection, log)
        {
            OnConnected();
        }

        public override Task Publish(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            action(Model);

            return Task.FromResult<object>(null);
        }
    }
}