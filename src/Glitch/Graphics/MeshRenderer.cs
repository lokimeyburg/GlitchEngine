﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System;
using Veldrid.ImageSharp;
using Veldrid.Utilities;
using Veldrid;
using Newtonsoft.Json;
using System.Collections.Generic;
using Glitch.Graphics;
using Glitch.Behaviors;
using Glitch.Assets;
using Glitch.Objects;

namespace Glitch.Graphics
{
    public class MeshRenderer : Component, ICullRenderable
    {
        private string _name;
        private MeshData _meshData;
        private AssetRef<MeshData> _meshAsset;
        private ImageSharpTexture _textureData;
        private AssetRef<ImageSharpTexture> _textureAsset;

        private readonly ImageSharpTexture _alphaTextureData;
        private Transform _transform = new Transform();

        [JsonIgnore]
        public Transform Transform => _transform;

        private BoundingBox _centeredBounds;
        private DeviceBuffer _vb;
        private DeviceBuffer _ib;
        private int _indexCount;
        private Texture _texture;
        private TextureView _textureView;
        private Texture _alphamapTexture;
        private TextureView _alphaMapView;

        private Pipeline _pipeline;
        private Pipeline _pipelineFrontCull;
        private ResourceSet _mainProjViewRS;
        private ResourceSet _mainSharedRS;
        private ResourceSet _mainPerObjectRS;
        // private ResourceSet _reflectionRS;
        // private ResourceSet _noReflectionRS;
        private Pipeline _shadowMapPipeline;
        private ResourceSet[] _shadowMapResourceSets;

        private DeviceBuffer _worldAndInverseBuffer;

        private readonly DisposeCollector _disposeCollector = new DisposeCollector();

        private readonly MaterialPropsAndBuffer _materialProps;
        private Vector3 _objectCenter;
        private bool _materialPropsOwned = false;

        // public MaterialProperties MaterialProperties { get => _materialProps.Properties; set { _materialProps.Properties = value; } }

        private AssetSystem _as;

        

        // Serialization Accessors
        public AssetRef<MeshData> Mesh
        {
            get { return _meshAsset; }
            set
            {
                _meshAsset = value;
            }
        }

        public AssetRef<ImageSharpTexture> Texture
        {
            get { return _textureAsset; }
            set
            {
                _textureAsset = value;
            }
        }

        [JsonConstructor]
        public MeshRenderer(AssetRef<MeshData> meshAsset, AssetRef<ImageSharpTexture> textureAsset)
        {
            _name = "TODO: Set random name";
            _meshAsset = meshAsset;
            _textureAsset = textureAsset;
            _materialProps = CommonMaterials.Brick;
        }

        public MeshRenderer(string name, MeshData meshData, ImageSharpTexture textureData, ImageSharpTexture alphaTexture, MaterialPropsAndBuffer materialProps)
        {
            _name = name;
            _meshData = meshData;
            _centeredBounds = meshData.GetBoundingBox();
            _objectCenter = _centeredBounds.GetCenter();
            _textureData = textureData;
            _alphaTextureData = alphaTexture;
            _materialProps = materialProps;
        }

        public BoundingBox BoundingBox() {
            // return _centeredBounds;
            return Veldrid.Utilities.BoundingBox.Transform(_centeredBounds, _transform.GetWorldMatrix());
        }

        public unsafe void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, GraphicsSystem sc)
        {
            ResourceFactory disposeFactory = new DisposeCollectorResourceFactory(gd.ResourceFactory, _disposeCollector);
            _vb = _meshData.CreateVertexBuffer(disposeFactory, cl);
            _vb.Name = _name + "_VB";
            _ib = _meshData.CreateIndexBuffer(disposeFactory, cl, out _indexCount);
            _ib.Name = _name + "_IB";

            _worldAndInverseBuffer = disposeFactory.CreateBuffer(new BufferDescription(64 * 2, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            if (_materialPropsOwned)
            {
                _materialProps.CreateDeviceObjects(gd, cl, sc);
            }

            if (_textureData != null)
            {
                _texture = StaticResourceCache.GetTexture2D(gd, gd.ResourceFactory, _textureData);
            }
            else
            {
                _texture = disposeFactory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                RgbaByte color = RgbaByte.Pink;
                gd.UpdateTexture(_texture, (IntPtr)(&color), 4, 0, 0, 0, 1, 1, 1, 0, 0);
            }

            _textureView = StaticResourceCache.GetTextureView(gd.ResourceFactory, _texture);

            if (_alphaTextureData != null)
            {
                _alphamapTexture = _alphaTextureData.CreateDeviceTexture(gd, disposeFactory);
            }
            else
            {
                _alphamapTexture = StaticResourceCache.GetPinkTexture(gd, gd.ResourceFactory);
            }
            _alphaMapView = StaticResourceCache.GetTextureView(gd.ResourceFactory, _alphamapTexture);

            VertexLayoutDescription[] shadowDepthVertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
            };

            (Shader depthVS, Shader depthFS) = StaticResourceCache.GetShaders(gd, gd.ResourceFactory, "ShadowDepth");

            ResourceLayout projViewCombinedLayout = StaticResourceCache.GetResourceLayout(
                gd.ResourceFactory,
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout worldLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription depthPD = new GraphicsPipelineDescription(
                BlendStateDescription.Empty,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(shadowDepthVertexLayouts, new[] { depthVS, depthFS }),
                new ResourceLayout[] { projViewCombinedLayout, worldLayout },
                sc.NearShadowMapFramebuffer.OutputDescription);
            _shadowMapPipeline = StaticResourceCache.GetPipeline(gd.ResourceFactory, ref depthPD);

            _shadowMapResourceSets = CreateShadowMapResourceSets(gd.ResourceFactory, disposeFactory, cl, sc, projViewCombinedLayout, worldLayout);

            VertexLayoutDescription[] mainVertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
            };

            (Shader mainVS, Shader mainFS) = StaticResourceCache.GetShaders(gd, gd.ResourceFactory, "ShadowMain");

            ResourceLayout projViewLayout = StaticResourceCache.GetResourceLayout(
                gd.ResourceFactory,
                StaticResourceCache.ProjViewLayoutDescription);

            ResourceLayout mainSharedLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("LightViewProjection1", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightViewProjection2", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightViewProjection3", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("DepthLimits", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("LightInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("CameraInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("PointLights", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

            ResourceLayout mainPerObjectLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("WorldAndInverse", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("MaterialProperties", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("RegularSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("AlphaMapSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapNear", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapMid", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapFar", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            // ResourceLayout reflectionLayout = StaticResourceCache.GetResourceLayout(gd.ResourceFactory, new ResourceLayoutDescription(
            //     new ResourceLayoutElementDescription("ReflectionMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            //     new ResourceLayoutElementDescription("ReflectionSampler", ResourceKind.Sampler, ShaderStages.Fragment),
            //     new ResourceLayoutElementDescription("ReflectionViewProj", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription mainPD = new GraphicsPipelineDescription(
                _alphamapTexture != null ? BlendStateDescription.SingleAlphaBlend : BlendStateDescription.SingleOverrideBlend,
                gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(mainVertexLayouts, new[] { mainVS, mainFS }),
                new ResourceLayout[] { projViewLayout, mainSharedLayout, mainPerObjectLayout },
                sc.MainSceneFramebuffer.OutputDescription);
            _pipeline = StaticResourceCache.GetPipeline(gd.ResourceFactory, ref mainPD);
            _pipeline.Name = "MeshRenderer Main Pipeline";
            mainPD.RasterizerState.CullMode = FaceCullMode.Front;
            mainPD.Outputs = sc.MainSceneFramebuffer.OutputDescription;
            _pipelineFrontCull = StaticResourceCache.GetPipeline(gd.ResourceFactory, ref mainPD);

            _mainProjViewRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(projViewLayout,
                sc.ProjectionMatrixBuffer,
                sc.ViewMatrixBuffer));

            _mainSharedRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(mainSharedLayout,
                sc.LightViewProjectionBuffer0,
                sc.LightViewProjectionBuffer1,
                sc.LightViewProjectionBuffer2,
                sc.DepthLimitsBuffer,
                sc.LightInfoBuffer,
                sc.CameraInfoBuffer,
                sc.PointLightsBuffer));

            _mainPerObjectRS = disposeFactory.CreateResourceSet(new ResourceSetDescription(mainPerObjectLayout,
                _worldAndInverseBuffer,
                _materialProps.UniformBuffer,
                _textureView,
                gd.Aniso4xSampler,
                _alphaMapView,
                gd.LinearSampler,
                sc.NearShadowMapView,
                sc.MidShadowMapView,
                sc.FarShadowMapView,
                gd.PointSampler));

            // _reflectionRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(reflectionLayout,
            //     _alphaMapView, // Doesn't really matter -- just don't bind the actual reflection map since it's being rendered to.
            //     gd.PointSampler,
            //     sc.ReflectionViewProjBuffer));

            // _noReflectionRS = StaticResourceCache.GetResourceSet(gd.ResourceFactory, new ResourceSetDescription(reflectionLayout,
            //     sc.ReflectionColorView,
            //     gd.PointSampler,
            //     sc.ReflectionViewProjBuffer));
        }

        private ResourceSet[] CreateShadowMapResourceSets(
            ResourceFactory sharedFactory,
            ResourceFactory disposeFactory,
            CommandList cl,
            GraphicsSystem sc,
            ResourceLayout projViewLayout,
            ResourceLayout worldLayout)
        {
            ResourceSet[] ret = new ResourceSet[6];

            for (int i = 0; i < 3; i++)
            {
                DeviceBuffer viewProjBuffer = i == 0 ? sc.LightViewProjectionBuffer0 : i == 1 ? sc.LightViewProjectionBuffer1 : sc.LightViewProjectionBuffer2;
                ret[i * 2] = StaticResourceCache.GetResourceSet(sharedFactory, new ResourceSetDescription(
                    projViewLayout,
                    viewProjBuffer));
                ResourceSet worldRS = disposeFactory.CreateResourceSet(new ResourceSetDescription(
                    worldLayout,
                    _worldAndInverseBuffer));
                ret[i * 2 + 1] = worldRS;
            }

            return ret;
        }

        public void DestroyDeviceObjects()
        {
            if (_materialPropsOwned)
            {
                _materialProps.DestroyDeviceObjects();
            }

            _disposeCollector.DisposeAll();
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(
                _pipeline.GetHashCode(),
                Vector3.Distance((_objectCenter * _transform.Scale) + _transform.Position, cameraPosition));
        }

        public RenderPasses RenderPasses()
        {
            if (_alphaTextureData != null)
            {
                return Glitch.Graphics.RenderPasses.AllShadowMap | Glitch.Graphics.RenderPasses.AlphaBlend | Glitch.Graphics.RenderPasses.ReflectionMap;
            }
            else
            {
                return Glitch.Graphics.RenderPasses.AllShadowMap | Glitch.Graphics.RenderPasses.Standard | Glitch.Graphics.RenderPasses.ReflectionMap;
            }
        }

        public void Render(GraphicsDevice gd, CommandList cl, GraphicsSystem sc, RenderPasses renderPass)
        {
            if (_materialPropsOwned)
            {
                _materialProps.FlushChanges(cl);
            }

            if ((renderPass & Glitch.Graphics.RenderPasses.AllShadowMap) != 0)
            {
                int shadowMapIndex = renderPass == Glitch.Graphics.RenderPasses.ShadowMapNear ? 0 : renderPass == Glitch.Graphics.RenderPasses.ShadowMapMid ? 1 : 2;
                RenderShadowMap(cl, sc, shadowMapIndex);
            }
            else if (renderPass == Glitch.Graphics.RenderPasses.Standard || renderPass == Glitch.Graphics.RenderPasses.AlphaBlend)
            {
                RenderStandard(cl, sc, false);
            }
            else if (renderPass == Glitch.Graphics.RenderPasses.ReflectionMap)
            {
                RenderStandard(cl, sc, true);
            }
        }

        public void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, GraphicsSystem sc)
        {
            WorldAndInverse wai;
            wai.World = _transform.GetWorldMatrix();
            wai.InverseWorld = VdUtilities.CalculateInverseTranspose(ref wai.World);
            gd.UpdateBuffer(_worldAndInverseBuffer, 0, ref wai);
        }

        private void RenderShadowMap(CommandList cl, GraphicsSystem sc, int shadowMapIndex)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(_shadowMapPipeline);
            cl.SetGraphicsResourceSet(0, _shadowMapResourceSets[shadowMapIndex * 2]);
            cl.SetGraphicsResourceSet(1, _shadowMapResourceSets[shadowMapIndex * 2 + 1]);
            cl.DrawIndexed((uint)_indexCount, 1, 0, 0, 0);
        }

        private void RenderStandard(CommandList cl, GraphicsSystem sc, bool reflectionPass)
        {
            cl.SetVertexBuffer(0, _vb);
            cl.SetIndexBuffer(_ib, IndexFormat.UInt16);
            cl.SetPipeline(reflectionPass ? _pipelineFrontCull : _pipeline);
            cl.SetGraphicsResourceSet(0, _mainProjViewRS);
            cl.SetGraphicsResourceSet(1, _mainSharedRS);
            cl.SetGraphicsResourceSet(2, _mainPerObjectRS);
            // cl.SetGraphicsResourceSet(3, reflectionPass ? _reflectionRS : _noReflectionRS);
            cl.DrawIndexed((uint)_indexCount, 1, 0, 0, 0);
        }


        public void Dispose()
        {
            DestroyDeviceObjects();
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            // return false
            return visibleFrustum.Contains(BoundingBox()) == ContainmentType.Disjoint;
        }

        protected override void Attached(SystemRegistry registry)
        {
            _as = registry.GetSystem<AssetSystem>();
            _meshData = _as.Database.LoadAsset<MeshData>(_meshAsset.ID);
            _textureData= _as.Database.LoadAsset<ImageSharpTexture>(_textureAsset.ID);
            _centeredBounds = _meshData.GetBoundingBox();
            _objectCenter = _centeredBounds.GetCenter();
            _transform = GameObject.Transform;

            // _gs = registry.GetSystem<GraphicsSystem>();
            // _ad = registry.GetSystem<AssetSystem>().Database;
            // _texture = Texture.Get(_ad);
            // _mesh = Mesh.Get(_ad);
            // _centeredBoundingSphere = _mesh.GetBoundingSphere();
            // _centeredBoundingBox = _mesh.GetBoundingBox();
            // _gs.ExecuteOnMainThread(() =>
            // {
            //     InitializeContextObjects(_gs.Context, _gs.MaterialCache, _gs.BufferCache);
            // });
        }

        protected override void Removed(SystemRegistry registry)
        {
            DestroyDeviceObjects();
        }

        protected override void OnEnabled()
        {
            // _gs.AddRenderItem(this, Transform);
        }

        protected override void OnDisabled()
        {
            // _gs.RemoveRenderItem(this);
        }


    }

    public struct WorldAndInverse
    {
        public Matrix4x4 World;
        public Matrix4x4 InverseWorld;
    }
}
