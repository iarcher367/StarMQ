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
    public interface ICommandDispatcher : IDisposable
    {
        Task Invoke(Action<IModel> action);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly ILog _log;
        private readonly IChannel _channel;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public CommandDispatcher(IChannel channel, ILog log)
        {
            _log = log;
            _channel = channel;

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
                            if (!_tokenSource.IsCancellationRequested)
                            {
                                action();

                                _log.Debug("Action processed.");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Info("Dispatcher terminated.");
                    }
                }, TaskCreationOptions.LongRunning);
        }

        public Task Invoke(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var tcs = new TaskCompletionSource<object>();

            _queue.Add(() =>
                {
                    if (_tokenSource.IsCancellationRequested)
                    {
                        tcs.SetCanceled();
                        return;
                    }

                    try
                    {
                        _channel.InvokeChannelAction(action);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, _tokenSource.Token);

            _log.Debug("Action added to queue.");

            return tcs.Task;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _channel.Dispose();

            _log.Info("Disposal complete.");
        }
    }
}