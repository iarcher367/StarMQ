namespace StarMQ.Core
{
    using log4net;
    using RabbitMQ.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// All publishes are done over a single channel and on a single thread to enforce clear ownership
    /// of thread-unsafe IModel instances; see RabbitMQ .NET client documentation section 2.10. A
    /// long-running thread is used to dispatch commands, preventing RabbitMQ from blocking the main
    /// application when it exerts TCP back-pressure.
    /// </summary>
    public interface IOutboundDispatcher : IDisposable
    {
        Task Invoke(Action<IModel> action);
    }

    public class OutboundDispatcher : IOutboundDispatcher
    {
        private readonly IChannel _channel;
        private readonly ILog _log;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ManualResetEvent _signal = new ManualResetEvent(true);
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private bool _disposed;

        public OutboundDispatcher(IChannel channel, IConnection connection, ILog log)
        {
            _channel = channel;
            _log = log;

            Dispatch();

            connection.OnDisconnected += OnDisconnected;
            connection.OnConnected += OnConnected;
        }

        private void Dispatch()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        foreach (var action in _queue.GetConsumingEnumerable(_tokenSource.Token))
                        {
                            _signal.WaitOne(-1);

                            action();

                            _log.Debug("Action processed.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Info("Dispatch cancelled.");
                    }
                }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void OnConnected()
        {
            _signal.Set();

            _log.Warn("Dispatch unblocked.");
        }

        private void OnDisconnected()
        {
            _signal.Reset();

            _log.Warn("Dispatch blocked.");
        }

        public Task Invoke(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            _queue.Add(() =>
            {
                try
                {
                    _channel.InvokeChannelAction(action);

                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            _log.Debug("Action added to queue.");

            return tcs.Task;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _queue.CompleteAdding();
            _tokenSource.Cancel();
            _channel.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}