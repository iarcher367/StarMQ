namespace StarMQ.Model
{
    public class Exchange
    {
        public string Name { get; private set; }

        /// <summary>
        /// Set true to delete this exchange when no queues are bound. Cannot be changed after creation.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Set true to prevent broker restarts from purging the exchange.
        ///
        /// Default true.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Set true to check if an exchange with the same name exists and throw an exception if not.
        /// </summary>
        public bool Passive { get; set; }

        public ExchangeType Type { get; set; }

        #region Args
        /// <summary>
        /// Set to have unroutable messages forwarded to an alternate exchange.
        /// </summary>
        public string AlternateExchangeName { get; set; }
        #endregion

        public Exchange(string name)
        {
            Name = Global.Validate("name", name);
            Durable = true;
        }
    }
}
