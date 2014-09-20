namespace StarMQ.Model
{
    public class Exchange
    {
        public string Name { get; private set; }

        public bool AutoDelete { get; set; }
        public bool Durable { get; set; }

        /// <summary>
        /// Set true to check if an exchange with the same name exists and throw an exception if not.
        /// </summary>
        public bool Passive { get; set; }

        public ExchangeType Type { get; set; }

        #region Args
        public string AlternateExchangeName { get; set; }
        #endregion

        public Exchange()
        {
            Durable = true;
        }

        #region Fluent
        public Exchange WithName(string name)
        {
            Name = Global.Validate("name", name);
            return this;
        }

        /// <summary>
        /// Set true to delete this exchange when no queues are bound. Cannot be changed after creation.
        /// </summary>
        public Exchange WithAutoDelete(bool autoDelete)
        {
            AutoDelete = autoDelete;
            return this;
        }

        /// <summary>
        /// Set true to prevent broker restarts from purging the exchange.
        ///
        /// Default true.
        /// </summary>
        public Exchange WithDurable(bool durable)
        {
            Durable = durable;
            return this;
        }

        // TODO: support configurable Type

        /// <summary>
        /// Set to have unroutable messages forwarded to an alternate exchange.
        /// </summary>
        public Exchange WithAlternateExchangeName(string name)
        {
            AlternateExchangeName = Global.Validate("name", name);
            return this;
        }
        #endregion
    }
}