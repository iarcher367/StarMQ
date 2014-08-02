namespace StarMQ.Message
{
    using System;

    public interface ITypeNameSerializer
    {
        Type Deserialize(string name);
        string Serialize(Type type);
    }
}