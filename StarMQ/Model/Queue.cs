namespace StarMQ.Model
{
    public class Queue
    {
        public string Name { get; private set; }

        /// <summary>
        /// Set true to have queue deleted when all consumers have disconnected or closed their channel.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Set true to prevent broker restarts from purging the queue.
        ///
        /// Default true.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Set true to make this queue only accessible by this connection.
        ///
        /// Note: when connection is lost, the queue is deleted!
        /// </summary>
        public bool Exclusive { get; set; }

        /// <summary>
        /// Set true to check if a queue with the same name exists and throw an exception if not.
        /// </summary>
        public bool Passive { get; set; }

        #region Args
        /// <summary>
        /// Set to enable dead-lettering.
        /// </summary>
        public string DeadLetterExchangeName { get; set; }

        /// <summary>
        /// Set to overwrite dead-lettered message's routing key
        /// </summary>
        public string DeadLetterExchangeRoutingKey { get; set; }

        /// <summary>
        /// Set to have queue deleted if it has no consumers, has not been re-declared, and has not
        /// had a basic.get invoked for specified duration in milliseconds.
        ///
        /// Cannot be zero.
        /// </summary>
        public uint Expires { get; set; }

        /// <summary>
        /// Set to specify time to live for messages in milliseconds.
        ///
        /// If zero, message is expired if it cannot be immediately delivered.
        /// </summary>
        public uint MessageTimeToLive { get; set; }
        #endregion

        public Queue(string name)
        {
            Name = Global.Validate("name", name);
            Durable = true;
            MessageTimeToLive = uint.MaxValue;
        }
    }
}