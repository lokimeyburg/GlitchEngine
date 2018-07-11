using Newtonsoft.Json;

namespace Glitch.Assets
{
    public class AssetRef<T>
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public AssetID ID { get; set; }

        [JsonConstructor]
        public AssetRef(AssetID id)
        {
            ID = id;
        }

        public AssetRef()
        {
        }
    }
}