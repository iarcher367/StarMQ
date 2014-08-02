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
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Net.Sockets;
    using System.Threading;

    public interface IConnection : IDisposable
    {
        bool IsConnected { get; }
        IModel CreateModel();

        event Action OnConnected;
        event Action OnDisconnected;
    }

    /// <summary>
    /// Opens a single connection to Rabbit and attempts to recover the connection if disconnected.
    /// </summary>
    public class PersistentConnection : IConnection
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly ConnectionFactory _factory;
        private readonly ILog _log;

        private RabbitMQ.Client.IConnection _connection;
        private bool _disposed;

        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected
        {
            get { return _connection.IsOpen && !_disposed; }
        }

        public PersistentConnection(IConnectionConfiguration configuration, ConnectionFactory factory,
            ILog log)
        {
            _configuration = configuration;
            _factory = factory;
            _log = log;

            Connect();
        }

        #region Connect
        private void Connect()
        {
            if (_disposed) return;

            _log.Info(String.Format("Attempting connection to {0}:{1}:{2}", _factory.HostName,
                Convert.ToString(_factory.Port), Convert.ToString(_factory.VirtualHost)));

            try
            {
                CreateConnection();
            }
            catch (BrokerUnreachableException ex)
            {
                _log.Error(String.Format("Unable to reach broker. Reconnecting in {0} ms.",
                    _configuration.Reconnect), ex);

                Retry();
            }
            catch (SocketException ex)
            {
                _log.Error(String.Format("Network error. Reconnecting in {0} ms.",
                    _configuration.Reconnect), ex);

                Retry();
            }
        }

        private void CreateConnection()
        {
            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;

            var onConnected = OnConnected;
            if (onConnected != null)
                onConnected();

            _log.Info("Connection to broker established.");
        }

        private void OnConnectionShutdown(RabbitMQ.Client.IConnection _, ShutdownEventArgs args)
        {
            _log.Info("Lost connection to broker.\n " + _connection.CloseReason);

            var onDisconnected = OnDisconnected;
            if (onDisconnected != null)
                onDisconnected();

            Connect();
        }

        private void Retry()
        {
            Thread.Sleep(_configuration.Reconnect);

            Connect();
        }
        #endregion

        public IModel CreateModel()
        {
            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _connection.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}