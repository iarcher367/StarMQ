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
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;

    public interface ISerializer
    {
        byte[] ToBytes<T>(T content) where T : class;
        dynamic ToObject(byte[] content, Type type);
    }

    public class JsonSerializer : ISerializer
    {
        public byte[] ToBytes<T>(T content) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");

            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, content);
                return stream.ToArray();
            }
        }

        public dynamic ToObject(byte[] content, Type type)
        {
            if (content == null || content.Length == 0)
                throw new ArgumentNullException("content");

            using (var stream = new MemoryStream())
            {
                stream.Write(content, 0, content.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var obj = new DataContractJsonSerializer(type).ReadObject(stream);
                return Convert.ChangeType(obj, type);
            }
        }
    }

    //using Newtonsoft.Json;

    //public class JsonSerializer : ISerializer
    //{
    //    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    //    {
    //        TypeNameHandling = TypeNameHandling.Auto
    //    };

    //    public byte[] ToBytes<T>(T content) where T : class
    //    {
    //        if (content == null)
    //            throw new ArgumentNullException("content");

    //        var json = JsonConvert.SerializeObject(content, _settings);
    //        return Encoding.UTF8.GetBytes(json);
    //    }

    //    public dynamic ToObject(byte[] content, Type type)
    //    {
    //        if (content == null || content.Length == 0)
    //            throw new ArgumentNullException("content");

    //        var json = Encoding.UTF8.GetString(content);
    //        return JsonConvert.DeserializeObject(json, type, _settings);
    //    }
    //}
}