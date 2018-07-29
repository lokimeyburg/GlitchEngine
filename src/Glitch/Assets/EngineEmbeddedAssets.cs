using System;
using System.Reflection;
using Veldrid;
using Veldrid.Utilities;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;
using Glitch.Graphics;

namespace Glitch.Assets
{
    public class EngineEmbeddedAssets : EmbeddedAssetDatabase
    {
        private static Assembly s_engineAssembly = typeof(EngineEmbeddedAssets).GetTypeInfo().Assembly;

        public static readonly AssetID PlaneModelID = "Internal:PlaneModel";
        public static readonly AssetID SphereModelID = "Internal:SphereModel";
        public static readonly AssetID CubeModelID = "Internal:CubeModel";

        public static readonly AssetID PinkTextureID = "Internal:PinkTexture";

        public static readonly AssetID SkyboxBackID = "Internal:SkyboxBack";
        public static readonly AssetID SkyboxFrontID = "Internal:SkyboxFront";
        public static readonly AssetID SkyboxLeftID = "Internal:SkyboxLeft";
        public static readonly AssetID SkyboxRightID = "Internal:SkyboxRight";
        public static readonly AssetID SkyboxBottomID = "Internal:SkyboxBottom";
        public static readonly AssetID SkyboxTopID = "Internal:SkyboxTop";

        public EngineEmbeddedAssets()
        {
            RegisterAsset(PlaneModelID, PlaneModel.MeshData);
            RegisterAsset(SphereModelID, SphereModel.MeshData);
            RegisterAsset(CubeModelID, CubeModel.MeshData);
            // RegisterAsset(PinkTextureID, CreatePinkTexture());
            RegisterSkyboxTextures();
        }

        private void RegisterSkyboxTextures()
        {
            Lazy<ImageSharpTexture> skyboxBack = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_bk.png"));
            Lazy<ImageSharpTexture> skyboxBottom = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_dn.png"));
            Lazy<ImageSharpTexture> skyboxFront = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_ft.png"));
            Lazy<ImageSharpTexture> skyboxLeft = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_lf.png"));
            Lazy<ImageSharpTexture> skyboxRight = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_rt.png"));
            Lazy<ImageSharpTexture> skyboxTop = new Lazy<ImageSharpTexture>(
                () => LoadEmbeddedTexture("Glitch.Assets.Textures.cloudtop.stormydays_up.png"));

            RegisterAsset(SkyboxBackID, skyboxBack);
            RegisterAsset(SkyboxFrontID, skyboxFront);
            RegisterAsset(SkyboxLeftID, skyboxLeft);
            RegisterAsset(SkyboxRightID, skyboxRight);
            RegisterAsset(SkyboxBottomID, skyboxBottom);
            RegisterAsset(SkyboxTopID, skyboxTop);
        }

        private static ImageSharpTexture LoadEmbeddedTexture(string embeddedName)
        {
            // return new ImageSharpTexture(embeddedName);
            using (var stream = s_engineAssembly.GetManifestResourceStream(embeddedName))
            {

                // LoadTexture(AssetHelper.GetPath("Models/gray.png"), true);
                return new ImageSharpTexture(Image.Load(stream));
            }
        }
    }
}
