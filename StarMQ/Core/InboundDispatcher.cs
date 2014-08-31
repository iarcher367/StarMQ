namespace StarMQ.Core
{
    using log4net;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IInboundDispatcher : IDisposable
    {
        Task Invoke(Action action);
    }

    public class InboundDispatcher : IInboundDispatcher
    {
        private readonly ILog _log;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ManualResetEvent _signal = new ManualResetEvent(true);
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private bool _disposed;

        public InboundDispatcher(IConnection connection, ILog log)
        {
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
                            action();

                            _log.Debug("Action processed.");

                            _signal.WaitOne(-1);
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
            Action action;

            while (_queue.TryTake(out action))
            {
                _log.Info("Message discarded.");
            }

            _signal.Reset();

            _log.Warn("Dispatch blocked.");
        }

        public Task Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            _queue.Add(() =>
            {
                try
                {
                    action();

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

            _log.Info("Dispose completed.");
        }
    }
}