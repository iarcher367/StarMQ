namespace StarMQ.Message
{
    using Exception;
    using System;

    public interface ITypeNameSerializer
    {
        Type Deserialize(string name);
        string Serialize(Type type);
    }

    public class TypeNameSerializer : ITypeNameSerializer
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