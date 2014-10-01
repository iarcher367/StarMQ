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

    internal class ConnectionConfiguration : IConnectionConfiguration
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