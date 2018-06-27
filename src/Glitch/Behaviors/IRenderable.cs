using System;
using System.Numerics;
using Veldrid.Utilities;
using Veldrid;

namespace Glitch.Graphics
{
    public interface IRenderable
    {
        void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, SceneContext sc);
        void Render(GraphicsDevice gd, CommandList cl, SceneContext sc, RenderPasses renderPass);
        void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, SceneContext sc);
        void DestroyDeviceObjects();
        void Dispose();
        RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);

        // RenderPasses RenderPasses => RenderPasses.Standard;
    }

}
