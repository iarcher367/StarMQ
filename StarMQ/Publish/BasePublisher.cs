namespace StarMQ.Publish
{
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System;
    using System.Threading.Tasks;
    using IConnection = Core.IConnection;

    public delegate void BasicReturnHandler(object sender, EventArgs args);

    public interface IPublisher
    {
        Task Publish(Action<IModel> action);

        event BasicReturnHandler BasicReturn;
    }

    /// <summary>
    /// All publishes are done over a single channel and on a single thread to enforce clear ownership
    /// of thread-unsafe IModel instances; see RabbitMQ .NET client documentation section 2.10. 
    /// </summary>
    public abstract class BasePublisher : IPublisher
    {
        private readonly IConnection _connection;

        protected readonly ILog Log;
        protected IModel Model;

        public event BasicReturnHandler BasicReturn;

        protected BasePublisher(IConnection connection, ILog log)
        {
            _connection = connection;
            Log = log;

            _connection.OnConnected += OnConnected;
            _connection.OnDisconnected += OnDisconnected;
        }

        protected virtual void OnConnected()
        {
            Model = _connection.CreateModel();
            Model.BasicReturn += HandleBasicReturn;

            Log.Info("Channel opened.");
        }

        protected virtual void OnDisconnected()
        {
            Model.BasicReturn -= HandleBasicReturn;
        }

        private void HandleBasicReturn(IModel model, BasicReturnEventArgs args)
        {
            const string format = "Basic.Return received for message with correlationId '{0}' " +
                                  "from exchange '{1}' with code '{2}:{3}'";

            Log.Warn(String.Format(format, args.BasicProperties.CorrelationId, args.Exchange,
                args.ReplyCode, args.ReplyText));

            var basicReturn = BasicReturn;
            if (basicReturn != null)
                basicReturn(model, args);
        }

        public abstract Task Publish(Action<IModel> action);
    }
}
