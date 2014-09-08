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
        ushort Port { get; }
        ushort PrefetchCount { get; }
        bool PublisherConfirms { get; }

        /// <summary>
        /// in milliseconds
        /// </summary>
        int Timeout { get; }

        string Username { get; }
        string VirtualHost { get; }

        // TODO: CancelOnHaFailover, SSL, AMQPConnectionString
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public ushort Heartbeat { get; set; }
        public string Host { get; set; }
        public string Password { get; set; }
        public ushort Port { get; set; }
        public ushort PrefetchCount { get; set; }
        public bool PublisherConfirms { get; set; }
        public int Timeout { get; set; }
        public string Username { get; set; }
        public string VirtualHost { get; set; }

        public ConnectionConfiguration()
        {
            Heartbeat = 10;
            Host = "localhost";
            Password = "guest";
            Port = 5672;
            PrefetchCount = 50;                 // TODO: tweak to optimize subscriber performance
            PublisherConfirms = false;
            Timeout = 10000;
            Username = "guest";
            VirtualHost = "/";
        }
    }
}