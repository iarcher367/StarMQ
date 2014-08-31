namespace StarMQ.Core
{
    using log4net;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using System;
    using System.Threading;

    /// <summary>
    /// Represents the RabbitMQ.Client IModel interface.
    /// </summary>
    public interface IChannel : IDisposable
    {
        void InvokeChannelAction(Action<IModel> action);
    }

    public class PersistentChannel : IChannel
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IConnection _connection;
        private readonly ILog _log;

        private IModel _channel;

        public PersistentChannel(IConnectionConfiguration configuration, IConnection connection,
            ILog log)
        {
            _configuration = configuration;
            _connection = connection;
            _log = log;
        }

        public void InvokeChannelAction(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            InvokeChannelAction(action, DateTime.Now);

            _log.Debug("Channel action invoked.");
        }

        private void InvokeChannelAction(Action<IModel> action, DateTime startTime)
        {
            if (IsTimedOut(startTime))
                throw new TimeoutException("Channel operation request has timed out.");

            try
            {
                if (_channel == null || _channel.IsClosed)
                    OpenChannel();

                action(_channel);
            }
//            catch (OperationInterruptedException ex)
//            {
                // TODO: parse AMQP exception text, possible retry
//            }
            catch (Exception ex)    // TODO: limit scope to only channel exceptions
            {
                _log.Warn("Channel failed.", ex);

                OnDisconnect();
                WaitForReconnectOrTimeout(startTime);
                InvokeChannelAction(action, startTime);
            }
        }

        private bool IsTimedOut(DateTime startTime)
        {
            return startTime.AddSeconds(_configuration.Timeout) < DateTime.Now;
        }

        private void OpenChannel()
        {
            _channel = _connection.CreateModel();

            _log.Info("Channel opened.");
        }

        private void OnDisconnect()
        {
            if (_channel == null || _channel.IsClosed) return;

            _channel.Dispose();

            _log.Info("Channel disconnected.");
        }

        /// <summary>
        /// Attempts to reconnect using exponential backoff.
        /// </summary>
        private void WaitForReconnectOrTimeout(DateTime startTime)
        {
            _log.Info("Attempting to reconnect.");

            var delayInterval = 100;

            while (_channel != null && _channel.IsClosed && !IsTimedOut(startTime))
            {
                Thread.Sleep(Math.Min(delayInterval, 5000));
                delayInterval *= 2;

                try
                {
                    OpenChannel();
                }
                catch (OperationInterruptedException ex)
                {
                    _log.Warn("Channel reconnect attempt failed.", ex);
                }
            }
        }

        public void Dispose()
        {
            if (_channel == null) return;

            _channel.Dispose();

            _log.Info("Dispose completed.");
        }
    }
}