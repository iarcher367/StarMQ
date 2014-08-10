namespace StarMQ
{
    using Exception;
    using System;

    public class Global
    {
        public static string Validate(string field, string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Length > 255)
                throw new MaxLengthException(field, value);

            return value;
        }
    }
}
