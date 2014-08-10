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
        private readonly IConnection _connection;
        private readonly IConnectionConfiguration _configuration;
        private readonly ILog _log;

        private IModel _channel;

        public IModel Channel     // TODO: no external callers?
        {
            get
            {
                if (_channel == null)
                    OpenChannel();

                return _channel;
            }
        }

        public PersistentChannel(IConnectionConfiguration configuration, IConnection connection,
            ILog log)
        {
            _configuration = configuration;
            _connection = connection;
            _log = log;

            // TODO: event subscriptions
        }

        private void OnDisconnect()
        {
            if (_channel == null || _channel.IsClosed) return;

            _channel.Dispose();

            _log.Info("Channel disconnected.");
        }

        private void OpenChannel()
        {
            _channel = _connection.CreateModel();

            _log.Info("Channel opened.");
        }

        public void InvokeChannelAction(Action<IModel> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            _log.Debug("Channel action invoked.");

            InvokeChannelAction(action, DateTime.Now);
        }

        private void InvokeChannelAction(Action<IModel> action, DateTime startTime)
        {
            if (IsTimedOut(startTime))
                throw new TimeoutException("Channel operation request has timed out.");

            try
            {
                action(Channel);
            }
//            catch (OperationInterruptedException ex)
//            {
                // TODO: parse AMQP exception text, possible retry
//            }
            catch (Exception ex)
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

        /// <summary>
        /// Attempts to reconnect using exponential backoff.
        /// </summary>
        private void WaitForReconnectOrTimeout(DateTime startTime)
        {
            _log.Info("Attempting to reconnect.");

            var delayInterval = 10;

            while (_channel != null && _channel.IsClosed && !IsTimedOut(startTime))
            {
                Thread.Sleep(delayInterval);
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

            _log.Info("Disposal complete.");
        }
    }
}