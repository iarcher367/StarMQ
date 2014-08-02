namespace StarMQ.Message
{
    using Exception;
    using System;

    public class TypeNameSerializer : ITypeNameSerializer
    {
        public Type Deserialize(string name)
        {
            var parts = name.Split(':');

            if (parts.Length != 2)
                throw new StarMqException("{0} is not a valid type name. Expected Type:Assembly", name);

            var type = Type.GetType(parts[0] + "," + parts[1]);

            if (type == null)
                throw new StarMqException("Cannot find type {0}", name);

            return type;
        }

        public string Serialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var name = type.FullName + ":" + type.Assembly.GetName().Name;

            if (name.Length > 255)
                throw new MaxLengthException("type", name);

            return name;
        }
    }
}