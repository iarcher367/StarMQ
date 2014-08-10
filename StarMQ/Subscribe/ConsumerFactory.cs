namespace StarMQ.Subscribe
{
    using log4net;
    using Model;
    using RabbitMQ.Client;
    using System;
    using IConnection = Core.IConnection;

    // TODO: config: priority, cancelOnHaFailover, prefetchCount

    public class ConsumerFactory
    {
        public static IConsumer CreateConsumer(Queue queue, IConnection connection, ILog log)
        {
            return queue.Exclusive
                ? (IConsumer)new TransientConsumer(queue, connection, log)
                : new PersistentConsumer(queue, connection, log);
        }
    }

    public interface IConsumer
    {
        Queue Queue { get; }
        string Tag { get; }

        void Consume(); // TODO: roll into Constructor
    }

    public abstract class ConsumerBase : IConsumer, IDisposable
    {
        private readonly IConnection _connection;
        protected readonly ILog Log;

        private bool _disposed;

        public Queue Queue { get; private set; }
        public string Tag { get; private set; }

        protected ConsumerBase(Queue queue, IConnection connection, ILog log)
        {
            Queue = queue;
            Log = log;

            _connection = connection;
        }

        public abstract void Consume();

        private void ModelOnBasicCancel()
        {
            Cancel();

            Log.Info("Basic.Cancel received.");
        }

        private void ModelOnBasicCancelOk()
        {
            Cancel();

            Log.Info("Basic.CancelOk received - broker acknowledged client Basic.Cancel.");
        }

        private void Cancel()
        {
        }

        private void ModelOnBasicConsumeOk(string id)
        {
            Tag = id;
        }

        private void ModelOnShutdown(IModel model, ShutdownEventArgs args)
        {
            const string format = "Consumer '{0}' on queue '{1}' has been shutdown by '{2}'. Reason: '{3}'";
            // TODO: tag, queu.name
            Log.Info(String.Format(format, Tag, Queue.Name, args.Initiator, args.Cause));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            Log.Info("Disposal complete.");

            throw new NotImplementedException();
        }
    }
}
