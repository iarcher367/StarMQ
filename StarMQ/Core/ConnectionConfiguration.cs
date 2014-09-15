namespace StarMQ.Core
{

    public interface IConnectionConfiguration
    {
        bool CancelOnHaFailover { get; }

        //IDictionary<string, object> Capabilities { get; } // enabled by default in RabbitMQ .NET client

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
        int Reconnect { get; }

        /// <summary>
        /// in milliseconds
        /// </summary>
        int Timeout { get; }

        string Username { get; }
        string VirtualHost { get; }

        // TODO: SSL, AMQPConnectionString
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public bool CancelOnHaFailover { get; set; }
        public ushort Heartbeat { get; set; }
        public string Host { get; set; }
        public string Password { get; set; }
        public ushort Port { get; set; }
        public ushort PrefetchCount { get; set; }
        public bool PublisherConfirms { get; set; }
        public int Reconnect { get; set; }
        public int Timeout { get; set; }
        public string Username { get; set; }
        public string VirtualHost { get; set; }

        public ConnectionConfiguration()
        {
            CancelOnHaFailover = false;
            Heartbeat = 10;
            Host = "localhost";
            Password = "guest";
            Port = 5672;
            PrefetchCount = 50;                 // TODO: tweak to optimize subscriber performance
            PublisherConfirms = false;
            Reconnect = 5000;
            Timeout = 10000;
            Username = "guest";
            VirtualHost = "/";
        }
    }
}