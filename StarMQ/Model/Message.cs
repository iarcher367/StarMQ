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

    public interface IMessage<out T>
    {
        T Body { get; }
        Properties Properties { get; set; }
    }

    public class Message<T> : IMessage<T> where T : class
    {
        private Properties _properties;

        public T Body { get; private set; }

        public Properties Properties
        {
            get { return _properties; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _properties = value;
            }
        }

        public Message(T body)
        {
            if (body == null)
                throw new ArgumentNullException("body");

            Body = body;
            Properties = new Properties();
        }
    }
}