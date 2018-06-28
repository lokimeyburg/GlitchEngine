using System;
using System.Diagnostics;
using System.Numerics;
using Veldrid.Sdl2;
using Veldrid;
using Glitch.Behaviors;

namespace Glitch.Graphics
{
    public class ImGuiRenderable : IRenderable, IUpdateable
    {
        private ImGuiRenderer _imguiRenderer;
        private int _width;
        private int _height;

        public ImGuiRenderable(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void WindowResized(int width, int height) => _imguiRenderer.WindowResized(width, height);

        public void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(gd, sc.MainSceneFramebuffer.OutputDescription, _width, _height);
            }
            else
            {
                _imguiRenderer.CreateDeviceResources(gd, sc.MainSceneFramebuffer.OutputDescription);
            }
        }

        public void DestroyDeviceObjects()
        {
            _imguiRenderer.Dispose();
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        public void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass)
        {
            Debug.Assert(renderPass == Glitch.Graphics.RenderPasses.Overlay);
            _imguiRenderer.Render(gd, cl);
        }

        public void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc)
        {
        }

        public RenderPasses RenderPasses() {
            return Glitch.Graphics.RenderPasses.Overlay;
        }

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(deltaSeconds, InputTracker.FrameSnapshot);
        }

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }
}
