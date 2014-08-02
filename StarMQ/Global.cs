namespace StarMQ
{
    using Exception;

    public class Global
    {
        public static string Validate(string value, string name)
        {
            if (value == null)
                return null;
            if (value.Length > 255)
                throw new MaxLengthException(name, value);

            return value;
        }
    }
}
