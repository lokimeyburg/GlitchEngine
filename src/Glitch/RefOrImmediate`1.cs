using Newtonsoft.Json;
using System;
using Glitch.Assets;
using Veldrid;

namespace Glitch
{
    public struct RefOrImmediate<T>
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        private AssetRef<T> _ref;
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        private T _value;

        public bool HasValue => _ref == null;

        public AssetRef<T> GetRef()
        {
            return _ref;
        }

        [JsonConstructor]
        public RefOrImmediate(AssetRef<T> reference, T value)
        {
            _ref = reference;
            _value = value;
        }

        public T Get(AssetDatabase ad)
        {
            if (_ref != null)
            {
                return ad.LoadAsset(_ref);
            }
            else
            {
                return _value;
            }
        }

        public static implicit operator RefOrImmediate<T>(T value) => new RefOrImmediate<T>(null, value);
        public static implicit operator RefOrImmediate<T>(AssetRef<T> reference) => new RefOrImmediate<T>(reference, default(T));
        public static implicit operator RefOrImmediate<T>(AssetID id) => new RefOrImmediate<T>(new AssetRef<T>(id), default(T));
        public static implicit operator RefOrImmediate<T>(string reference) => new RefOrImmediate<T>(new AssetRef<T>(reference), default(T));
    }
}
