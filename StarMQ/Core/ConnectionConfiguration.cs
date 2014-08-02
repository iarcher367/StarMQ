namespace StarMQ.Core
{
    public interface IConnectionConfiguration
    {
        /// <summary>
        /// in seconds
        /// </summary>
        ushort Heartbeat { get; }

        string Host { get; }
        string Password { get; }
        bool PersistMessages { get; set; }
        ushort Port { get; }
        ushort PrefetchCount { get; }
        bool PublisherConfirms { get; }

        /// <summary>
        /// in seconds
        /// </summary>
        ushort Timeout { get; }

        string Username { get; }
        string VirtualHost { get; }

        // TODO: CancelOnHaFailover, SSL, AMQPConnectionString
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public ushort Heartbeat { get; set; }
        public string Host { get; set; }
        public string Password { get; set; }
        public bool PersistMessages { get; set; }
        public ushort Port { get; set; }
        public ushort PrefetchCount { get; set; }
        public bool PublisherConfirms { get; set; }
        public ushort Timeout { get; set; }
        public string Username { get; set; }
        public string VirtualHost { get; set; }

        public ConnectionConfiguration()        // TODO: read from appSettings
        {
            Heartbeat = 10;
            Host = "Lx0711";
            Password = "password";
            PersistMessages = true;
            Port = 5672;
            PrefetchCount = 50;     // tweak this value to optimize subscriber performance
            PublisherConfirms = false;
            Timeout = 10;
            Username = "admin";
            VirtualHost = "/";
        }
    }
}