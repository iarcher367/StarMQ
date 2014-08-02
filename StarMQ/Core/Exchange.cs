namespace StarMQ.Core
{
    public class Exchange
    {
        public string Name { get; private set; }

        /// <summary>
        /// Set true to delete this exchange when no queues are bound. Cannot be changed after creation.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Set true to prevent server restarts from purging the exchange. Default is true.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Set true to check if an exchange with the same name exists and throw an exception if not.
        /// </summary>
        public bool Passive { get; set; }

        public ExchangeType Type { get; set; }

        public Exchange(string name)
        {
            Global.Validate(name, "name");
            Name = name;
            Durable = true;
        }
    }
}
