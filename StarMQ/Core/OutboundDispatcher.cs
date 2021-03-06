﻿#region Apache License v2.0
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
    using System;
    using System.Collections.Concurrent;
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
        Task Invoke(Action<IConnection> action);
    }

    internal class OutboundDispatcher : IOutboundDispatcher
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly ILog _log;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly ManualResetEvent _dispatchSignal = new ManualResetEvent(true);
        private readonly ManualResetEvent _disposeSignal = new ManualResetEvent(false);

        private bool _disposed;

        public OutboundDispatcher(IConnectionConfiguration configuration, IConnection connection,
            ILog log)
        {
            _configuration = configuration;
            _connection = connection;
            _log = log;

            Dispatch();

            connection.OnConnected += OnConnected;
            connection.OnDisconnected += OnDisconnected;
        }

        private void Dispatch()
        {
            Task.Run(() =>
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                {
                    action();

                    _log.Debug("Action processed.");
                }

                _disposeSignal.Set();
            });
        }

        private void OnConnected()
        {
            _dispatchSignal.Set();
            _log.Info("Dispatch unblocked.");
        }

        private void OnDisconnected()
        {
            _dispatchSignal.Reset();
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

        public Task Invoke(Action<IConnection> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            return Invoke(() => action(_connection));
        }

        private void InvokeAction(Action action)
        {
            var retryInterval = 100;

            while (true)
            {
                try
                {
                    _dispatchSignal.WaitOne(-1);

                    action();
                    return;
                }
                catch (Exception ex)
                {
                    _log.Warn(String.Format("Channel failed - retrying in {0} ms.", retryInterval)
                        , ex);

                    Thread.Sleep(retryInterval);
                    retryInterval = Math.Min(retryInterval * 2, _configuration.Reconnect);
                }
                //catch (IOException ex)
                //catch (NotSupportedException ex)
                //catch (OperationInterruptedException ex) ?
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _queue.CompleteAdding();
            _disposeSignal.WaitOne();
            _connection.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}