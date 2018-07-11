using Newtonsoft.Json;
using System.IO;
using System;

namespace Glitch.Assets
{
    public class TextAssetLoader<T> : ConcreteLoader<T>
    {
        private readonly JsonSerializer _serializer;

        public override string FileExtension => "json";

        public TextAssetLoader(JsonSerializer serializer)
        {
            _serializer = serializer;           
        }

        public override T Load(Stream s)
        {
            using (var sr = new StreamReader(s))
            using (var jtr = new JsonTextReader(sr))
            {
                return _serializer.Deserialize<T>(jtr);
            }
        }
    }
}