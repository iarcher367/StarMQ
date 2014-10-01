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
    public class Exchange
    {
        internal string Name { get; private set; }

        internal bool AutoDelete { get; set; }
        internal bool Durable { get; set; }

        /// <summary>
        /// Set true to check if an exchange with the same name exists and throw an exception if not.
        /// </summary>
        internal bool Passive { get; set; }

        internal ExchangeType Type { get; set; }

        #region Args
        internal string AlternateExchangeName { get; set; }
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