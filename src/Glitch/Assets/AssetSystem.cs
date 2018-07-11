using Glitch.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
// using SharpFont;
using System;
using System.IO;
using Veldrid;

namespace Glitch.Assets
{
    public class AssetSystem : GameSystem
    {
        private readonly AssetDatabase _ad;
        private string _assetRootPath;

        public AssetSystem(string assetRootPath, ISerializationBinder binder)
        {
            _assetRootPath = assetRootPath;
            _ad = CreateAssetDatabase(binder);
            LooseFileDatabase.AddExtensionTypeMapping(".scene", typeof(SceneAsset));
        }

        protected virtual AssetDatabase CreateAssetDatabase(ISerializationBinder binder)
        {
            var fileAssets = new LooseFileDatabase(_assetRootPath);
            fileAssets.DefaultSerializer.SerializationBinder = binder;
            // fileAssets.RegisterTypeLoader(typeof(WaveFile), new WaveFileLoader());
            // LooseFileDatabase.AddExtensionTypeMapping(".wav", typeof(WaveFile));
            // fileAssets.RegisterTypeLoader(typeof(FontFace), new FontFaceLoader());
            // LooseFileDatabase.AddExtensionTypeMapping(".ttf", typeof(FontFace));
            var embeddedAssets = new EngineEmbeddedAssets();
            var compoundDB = new CompoundAssetDatabase();
            compoundDB.AddDatabase(fileAssets);
            compoundDB.AddDatabase(embeddedAssets);
            return compoundDB;
        }

        public AssetDatabase Database => _ad;

        protected override void UpdateCore(float deltaSeconds)
        {
        }
    }
}