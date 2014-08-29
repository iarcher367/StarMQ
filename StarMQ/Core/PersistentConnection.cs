namespace StarMQ.Core
{
    using Exception;
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Net.Sockets;
    using System.Threading.Tasks;

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
        /// <summary>
        /// in milliseconds
        /// </summary>
        private const int RetryInterval = 5000;
        private const string RetryMsg = "Failed to connect to broker. Retrying in {0} ms.";

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

        public PersistentConnection(IConnectionConfiguration configuration, ILog log)
        {
            _factory = new ConnectionFactory
            {
                HostName = configuration.Host,
                Password = configuration.Password,
                Port = configuration.Port,
                UserName = configuration.Username,
                VirtualHost = configuration.VirtualHost,
                RequestedHeartbeat = configuration.Heartbeat,
                RequestedConnectionTimeout = configuration.Timeout * 1000
            };

            _log = log;   // TODO: consider EventBus

            Connect();
        }

        #region Connect
        private void Connect()
        {
            if (_disposed) return;

            _log.Info("Attempting to connect to broker.");

            try
            {
                CreateConnection();
            }
            catch (BrokerUnreachableException ex)
            {
                _log.Error(String.Format(RetryMsg, RetryInterval), ex);

                Retry();
            }
            catch (SocketException ex)
            {
                _log.Error(String.Format(RetryMsg, RetryInterval), ex);

                Retry();
            }
        }

        private void CreateConnection()
        {
            _log.Info(String.Format("Broker connection created to {0}:{1}:{2}", _factory.HostName,
                Convert.ToString(_factory.Port), Convert.ToString(_factory.VirtualHost)));

            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;

            var onConnected = OnConnected;
            if (onConnected != null)
                onConnected();

            _log.Info("Connection to broker established.");
        }

        private void OnConnectionShutdown(RabbitMQ.Client.IConnection _, ShutdownEventArgs args)
        {
            _log.Info("Lost connection to broker.");

            var onDisconnected = OnDisconnected;
            if (onDisconnected != null)
                onDisconnected();

            Connect();
        }

        private async void Retry()
        {
            await Task.Delay(RetryInterval);

            Connect();
        }
        #endregion

        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new StarMqException("Not connected.");

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _connection.Dispose();

            _log.Info("Disposal complete.");
        }
    }
}