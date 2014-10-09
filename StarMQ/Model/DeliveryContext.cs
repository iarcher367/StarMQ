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
    using System;

    public class DeliveryContext
    {
        public bool Redelivered { get; internal set; }
        public string RoutingKey { get; internal set; }
        public Properties Properties { get; internal set; }

        public DeliveryContext()
        {
            Properties = new Properties();
        }

        /// <summary>
        /// Set the message's routing key.
        /// </summary>
        public DeliveryContext WithRoutingKey(string key)
        {
            RoutingKey = Global.Validate("key", key);
            return this;
        }

        /// <summary>
        /// Adds a key-value pair to the message header.
        /// </summary>
        public DeliveryContext WithHeader(string key, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            Properties.Headers[Global.Validate("key", key)] = value;
            return this;
        }
    }
}