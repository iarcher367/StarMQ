namespace StarMQ.Consume
{
    using log4net;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsumerDispatcher : IDisposable
    {
        Task Invoke(Action action);
    }

    public class ConsumerDispatcher : IConsumerDispatcher   // TODO: DispatcherStrategy for TPL
    {
        private readonly ILog _log;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private bool _disposed;

        public ConsumerDispatcher(ILog log)
        {
            _log = log;

            Dispatch();
        }

        private void Dispatch()
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        foreach (var action in _queue.GetConsumingEnumerable(_tokenSource.Token))
                        {
                            _tokenSource.Token.ThrowIfCancellationRequested();

                            action();

                            _log.Debug("Action processed.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Info("Dispatcher terminated.");
                    }
                });
        }

        public Task Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            _queue.Add(() =>
            {
                _tokenSource.Token.ThrowIfCancellationRequested();

                try
                {
                    action();

                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, _tokenSource.Token);

            _log.Debug("Action added to queue");

            return tcs.Task;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _queue.CompleteAdding();
            _tokenSource.Cancel();

            _log.Info("Disposal complete.");
        }
    }
}