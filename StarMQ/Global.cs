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

namespace StarMQ
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

            foreach (var kvp in settings.Select(x => x.Split(new[] {'='})))
            {
                var property = configuration.GetType().GetProperty(kvp[0], flags);

                if (property == null)
                    throw new StarMqException("Unknown setting found in connection string: {0}", kvp[0]);

                property.SetValue(configuration, Convert.ChangeType(kvp[1], property.PropertyType));
            }
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
