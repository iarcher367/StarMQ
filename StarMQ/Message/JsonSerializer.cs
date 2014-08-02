namespace StarMQ.Message
{
    using Newtonsoft.Json;
    using System;
    using System.Text;

    public class JsonSerializer : ISerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

        public byte[] ToBytes<T>(T content) where T : class
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var json = JsonConvert.SerializeObject(content, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T ToObject<T>(byte[] content) where T : class
        {
            if (content == null || content.Length == 0)
                throw new ArgumentNullException("content");

            var json = Encoding.UTF8.GetString(content);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}