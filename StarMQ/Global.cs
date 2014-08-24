﻿namespace StarMQ
{
    using Core;
    using Exception;
    using System;
    using System.Linq;
    using System.Reflection;

    public class Global
    {
        public static void ParseConfiguration(IConnectionConfiguration configuration, string connectionString)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            var settings = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var kvp in settings.Select(x => x.Split(new[] { '=' })))
                configuration.GetType().GetProperty(kvp[0], flags).SetValue(configuration, kvp[1]);
        }

        public static string Validate(string field, string value)
        {
            if (field == null)
                throw new ArgumentNullException("field");
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Length > 255)
                throw new MaxLengthException(field, value);

            return value;
        }
    }
}
