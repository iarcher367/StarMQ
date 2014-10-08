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

namespace StarMQ.Model
{
    using System.Collections.Generic;

    public class Queue
    {
        internal string Name { get; private set; }

        internal bool AutoDelete { get; set; }
        internal bool Durable { get; set; }
        internal bool Exclusive { get; set; }

        /// <summary>
        /// Set true to check if a queue with the same name exists and throw an exception if not.
        /// </summary>
        internal bool Passive { get; set; }

        #region Args
        internal bool CancelOnHaFailover { get; set; }
        internal string DeadLetterExchangeName { get; set; }
        internal string DeadLetterRoutingKey { get; set; }
        internal uint Expires { get; set; }
        internal uint MessageTimeToLive { get; set; }
        internal int Priority { get; set; }
        #endregion

        internal readonly List<string> BindingKeys = new List<string>();

        public Queue()
        {
            Durable = true;
            MessageTimeToLive = uint.MaxValue;
        }

        #region Fluent
        public Queue WithName(string name)
        {
            Name = Global.Validate("name", name);
            return this;
        }

        /// <summary>
        /// Set true to have queue deleted when all consumers have disconnected or closed their channel.
        /// </summary>
        public Queue WithAutoDelete(bool autodelete)
        {
            AutoDelete = autodelete;
            return this;
        }

        /// <summary>
        /// Set true to prevent broker restarts from purging the queue.
        ///
        /// Default true.
        /// </summary>
        public Queue WithDurable(bool durable)
        {
            Durable = durable;
            return this;
        }

        /// <summary>
        /// Set true to make this queue only accessible by this connection.
        ///
        /// Note: when connection is lost, this queue is deleted!
        /// </summary>
        public Queue WithExclusive(bool exclusive)
        {
            Exclusive = exclusive;
            return this;
        }

        /// <summary>
        /// Set to receive basic.cancel when a mirrored queue queue fails over.
        /// </summary>
        public Queue WithCancelOnHaFailover(bool notify)
        {
            CancelOnHaFailover = notify;
            return this;
        }

        /// <summary>
        /// Set to specify dead letter exchange name.
        ///
        /// Default is auto-generated.
        /// </summary>
        public Queue WithDeadLetterExchangeName(string name)
        {
            DeadLetterExchangeName = Global.Validate("name", name);
            return this;
        }

        /// <summary>
        /// Set to overwrite dead-lettered message's routing key
        /// </summary>
        public Queue WithDeadLetterRoutingKey(string key)
        {
            DeadLetterRoutingKey = Global.Validate("key", key);
            return this;
        }

        /// <summary>
        /// Set to have queue deleted if it has no consumers, has not been re-declared, and has not
        /// had a basic.get invoked for specified duration in milliseconds.
        ///
        /// Cannot be zero.
        /// </summary>
        public Queue WithExpires(uint interval)
        {
            Expires = interval;
            return this;
        }

        /// <summary>
        /// Set to specify time to live for messages in milliseconds.
        ///
        /// If zero, message is expired if it cannot be immediately delivered.
        /// </summary>
        public Queue WithMessageTimeToLive(uint interval)
        {
            MessageTimeToLive = interval;
            return this;
        }

        /// <summary>
        /// Set to direct messages to highest priority consumers as long as they can receive them.
        /// Consumers with equivalent priority will round-robin.
        /// </summary>
        public Queue WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        /// <summary>
        /// Messages with routing keys that do not match any binding key are filtered out.
        ///
        /// # (hash) substitutes for zero or more words; e.g. lazy.#
        /// * (star) substitutes for exactly one word; e.g. *.fox
        /// </summary>
        public Queue WithBindingKey(string key)
        {
            BindingKeys.Add(Global.Validate("key", key));
            return this;
        }
        #endregion
    }
}