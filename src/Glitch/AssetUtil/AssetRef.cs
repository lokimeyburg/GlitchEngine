namespace Glitch.AssetUtil
{
    public class AssetRef<T>
    {
        public AssetID ID { get; set; }

        public AssetRef(AssetID id)
        {
            ID = id;
        }

        public AssetRef()
        {
        }
    }
}