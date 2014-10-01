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

namespace StarMQ.Message
{
    using Exception;
    using System;

    public interface ITypeNameSerializer
    {
        Type Deserialize(string name);
        string Serialize(Type type);
    }

    internal class TypeNameSerializer : ITypeNameSerializer
    {
        public Type Deserialize(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            var type = Type.GetType(name);

            if (type == null)
                throw new StarMqException("Cannot find type {0}", name);

            return type;
        }

        public string Serialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var name = type.AssemblyQualifiedName ?? String.Empty;

            if (name.Length > 255)
                throw new MaxLengthException("type", name);

            return name;
        }
    }
}