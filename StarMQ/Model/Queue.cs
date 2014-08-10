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

        /// <summary>
        /// Set to enable dead-lettering.
        /// </summary>
        public string DeadLetterExchangeName { get; set; }

        /// <summary>
        /// Set to overwrite dead-lettered message's routing key
        /// </summary>
        public string DeadLetterExchangeRoutingKey { get; set; }

        // TODO: perQueueTTL, expires

        public Queue(string name)
        {
            Name = Global.Validate("name", name);
            Durable = true;
        }
    }
}