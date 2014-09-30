#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ.Core
{
    using RabbitMQ.Client;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A long-running thread is used to dispatch commands, preventing RabbitMQ from blocking the
    /// main application when it exerts TCP back-pressure. This implementation allows commands to
    /// be buffered during connection failures.
    /// </summary>
    public interface IOutboundDispatcher : IDisposable
    {
        Task Invoke(Action action);
        Task Invoke(Action<IModel> action);
    }

    public class OutboundDispatcher : IOutboundDispatcher
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly ILog _log;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ManualResetEvent _signal = new ManualResetEvent(true);
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private bool _disposed;
        private IModel _model;

        public OutboundDispatcher(IConnectionConfiguration configuration, IConnection connection,
            ILog log)
        {
            _configuration = configuration;
            _connection = connection;
            _log = log;

            OpenChannel();
            Dispatch();

            connection.OnConnected += OnConnected;
            connection.OnDisconnected += OnDisconnected;
        }

        private void OpenChannel()
        {
            _model = _connection.CreateModel();

            _log.Info("Channel opened.");
        }

        private void Dispatch()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                {
                    action();

                    _log.Debug("Action processed.");
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void OnConnected()
        {
            OpenChannel();

            _signal.Set();

            _log.Info("Dispatch unblocked.");
        }

        private void OnDisconnected()
        {
            _signal.Reset();

            _log.Info("Dispatch blocked.");
        }

        public Task Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            _queue.Add(() => InvokeAction(action));

            _log.Debug("Action added to queue.");

            return Task.FromResult<object>(null);
        }

        public Task Invoke(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return Invoke(() => action(_model));
        }

        private void InvokeAction(Action action)
        {
            var retryInterval = 100;

            while (true)
            {
                try
                {
                    _signal.WaitOne(-1);

                    action();
                    return;
                }
                catch (IOException ex)
                {
                    _log.Warn(String.Format("Channel failed - retrying in {0} ms.", retryInterval)
                        , ex);

                    Thread.Sleep(retryInterval);
                    retryInterval = Math.Min(retryInterval * 2, _configuration.Reconnect);
                }
                //catch (NotSupportedException ex)
                //catch (OperationInterruptedException ex)
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _queue.CompleteAdding();
            _tokenSource.Cancel();
            _model.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}