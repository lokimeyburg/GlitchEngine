using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Utilities;
using Veldrid.ImageSharp;
using Veldrid;
using Glitch.Graphics;
using Glitch.Behaviors;
using Glitch.Assets;

namespace Glitch.Graphics
{
    public class Skybox : Component, IRenderable
    {
        private static ImageSharpTexture s_blankTexture = CreateBlankTexture();

        private AssetSystem _as;

        private RefOrImmediate<ImageSharpTexture> _front;
        private RefOrImmediate<ImageSharpTexture> _back;
        private RefOrImmediate<ImageSharpTexture> _left;
        private RefOrImmediate<ImageSharpTexture> _right;
        private RefOrImmediate<ImageSharpTexture> _top;
        private RefOrImmediate<ImageSharpTexture> _bottom;

        private static ImageSharpTexture CreateBlankTexture()
        {
            return new ImageSharpTexture(new Image<Rgba32>(1, 1));
        }

        // Context objects
        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private Pipeline _pipeline;
        private Pipeline _reflectionPipeline;
        private ResourceSet _resourceSet;
        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        // public Skybox(
        //     Image<Rgba32> front, Image<Rgba32> back, Image<Rgba32> left,
        //     Image<Rgba32> right, Image<Rgba32> top, Image<Rgba32> bottom)
        // {
        //     _front = front;
        //     _back = back;
        //     _left = left;
        //     _right = right;
        //     _top = top;
        //     _bottom = bottom;
        // }

        public Skybox(
            AssetRef<ImageSharpTexture> front, AssetRef<ImageSharpTexture> back, AssetRef<ImageSharpTexture> left,
            AssetRef<ImageSharpTexture> right, AssetRef<ImageSharpTexture> top, AssetRef<ImageSharpTexture> bottom)
        {
            _front = front;
            _back = back;
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        public unsafe void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            AssetDatabase ad = _as.Database;
            ResourceFactory factory = gd.ResourceFactory;

            _vb = factory.CreateBuffer(new BufferDescription(s_vertices.SizeInBytes(), BufferUsage.VertexBuffer));
            cl.UpdateBuffer(_vb, 0, s_vertices);

            _ib = factory.CreateBuffer(new BufferDescription(s_indices.SizeInBytes(), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(_ib, 0, s_indices);

            Texture textureCube;
            TextureView textureView;

            var front = !_front.HasValue ? _front.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxFrontID);
            var back = !_back.HasValue ? _back.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBackID);
            var left = !_left.HasValue ? _left.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxLeftID);
            var right = !_right.HasValue ? _right.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxRightID);
            var top = !_top.HasValue ? _top.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxTopID);
            var bottom = !_bottom.HasValue ? _bottom.Get(ad) : ad.LoadAsset<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBottomID);


            // front.Images[0].DangerousGetPinnableReferenceToPixelBuffer()

            // using (var frontPin = front.Pixels.Pin())
            // using (var backPin = back.Pixels.Pin())
            // using (var leftPin = left.Pixels.Pin())
            // using (var rightPin = right.Pixels.Pin())
            // using (var topPin = top.Pixels.Pin())
            // using (var bottomPin = bottom.Pixels.Pin())

            fixed (Rgba32* frontPin = &front.Images[0].DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* backPin = &back.Images[0].DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* leftPin = &left.Images[0].DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* rightPin = &right.Images[0].DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* topPin = &top.Images[0].DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* bottomPin = &bottom.Images[0].DangerousGetPinnableReferenceToPixelBuffer())

            {
                uint width = (uint)front.Width;
                uint height = (uint)front.Height;
                textureCube = factory.CreateTexture(TextureDescription.Texture2D(
                    width,
                    height,
                    1,
                    1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled | TextureUsage.Cubemap));

                uint faceSize = (uint)(front.Width * front.Height * Unsafe.SizeOf<Rgba32>());
                gd.UpdateTexture(textureCube, (IntPtr)rightPin, faceSize, 0, 0, 0, width, height, 1, 0, 0);
                gd.UpdateTexture(textureCube, (IntPtr)leftPin, faceSize, 0, 0, 0, width, height, 1, 0, 1);
                gd.UpdateTexture(textureCube, (IntPtr)topPin, faceSize, 0, 0, 0, width, height, 1, 0, 2);
                gd.UpdateTexture(textureCube, (IntPtr)bottomPin, faceSize, 0, 0, 0, width, height, 1, 0, 3);
                gd.UpdateTexture(textureCube, (IntPtr)backPin, faceSize, 0, 0, 0, width, height, 1, 0, 4);
                gd.UpdateTexture(textureCube, (IntPtr)frontPin, faceSize, 0, 0, 0, width, height, 1, 0, 5);

                textureView = factory.CreateTextureView(new TextureViewDescription(textureCube));
            }

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3))
            };

            (Shader vs, Shader fs) = StaticResourceCache.GetShaders(gd, gd.ResourceFactory, "Skybox");

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("CubeTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CubeSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { vs, fs }),
                new ResourceLayout[] { _layout },
                sc.MainSceneFramebuffer.OutputDescription);

            _pipeline = factory.CreateGraphicsPipeline(ref pd);
            pd.Outputs = sc.MainSceneFramebuffer.OutputDescription;
            _reflectionPipeline = factory.CreateGraphicsPipeline(ref pd);

            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _layout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer,
                textureView,
                gd.PointSampler));

            _disposeCollector.Add(_vb, _ib, textureCube, textureView, _layout, _pipeline, _reflectionPipeline, _resourceSet, vs, fs);
        }

        public void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
        }

        public void Dispose()
        {
            DestroyDeviceObjects();
        }

        public static Skybox LoadDefaultSkybox(SystemRegistry registry)
        {
            var assetSystem = registry.GetSystem<AssetSystem>();
            AssetDatabase ad = assetSystem.Database;

            AssetRef<ImageSharpTexture> front = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxFrontID);
            AssetRef<ImageSharpTexture> back = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBackID);
            AssetRef<ImageSharpTexture> left = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxLeftID);
            AssetRef<ImageSharpTexture> right = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxRightID);
            AssetRef<ImageSharpTexture> top = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxTopID);
            AssetRef<ImageSharpTexture> bottom = new AssetRef<ImageSharpTexture>(EngineEmbeddedAssets.SkyboxBottomID);

            // TODO: the Skybox should load the textures from the engine embedded assets
            var skybox =  new Skybox(front, back, left, right, top, bottom);
            skybox._as = assetSystem;
            return skybox;
        }

        public void DestroyDeviceObjects()
        {
            _disposeCollector.DisposeAll();
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _resourceSet);
            float depth = gd.IsDepthRangeZeroToOne ? 0 : 1;
            cl.SetViewport(0, new Viewport(0, 0, sc.MainSceneColorTexture.Width, sc.MainSceneColorTexture.Height, depth, depth));
            cl.DrawIndexed((uint)s_indices.Length, 1, 0, 0, 0);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public RefOrImmediate<ImageSharpTexture> Front
        {
            get { return _front; }
            set { _front = value; }
        }

        public RefOrImmediate<ImageSharpTexture> Back
        {
            get { return _back; }
            set { _back = value; }
        }

        public RefOrImmediate<ImageSharpTexture> Left
        {
            get { return _left; }
            set { _left = value; }
        }

        public RefOrImmediate<ImageSharpTexture> Right
        {
            get { return _right; }
            set { _right = value; }
        }

        public RefOrImmediate<ImageSharpTexture> Bottom
        {
            get { return _bottom; }
            set { _bottom = value; }
        }

        public RefOrImmediate<ImageSharpTexture> Top
        {
            get { return _top; }
            set { _top = value; }
        }

        // Component Implementation
        protected override void Attached(SystemRegistry registry)
        {
            _as = registry.GetSystem<AssetSystem>();
        }
        protected override void Removed(SystemRegistry registry)
        {

        }

        protected override void OnEnabled()
        {

        }

        protected override void OnDisabled()
        {

        }
        // END component implementation

        public RenderPasses RenderPasses() { 
            return Glitch.Graphics.RenderPasses.Standard | Glitch.Graphics.RenderPasses.ReflectionMap;
        }

        private static readonly VertexPosition[] s_vertices = new VertexPosition[]
        {
            // Top
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            // Bottom
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Left
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Right
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            // Back
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            // Front
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
        private ResourceLayout _layout;
    }
}
