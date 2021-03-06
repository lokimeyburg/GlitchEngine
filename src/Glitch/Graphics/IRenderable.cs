using System;
using System.Numerics;
using Veldrid.Utilities;
using Veldrid;

namespace Glitch.Graphics
{
    public interface IRenderable
    {
        void UpdatePerFrameResources(GraphicsDevice gd, CommandList cl, GraphicsSystem sc);
        void Render(GraphicsDevice gd, CommandList cl, GraphicsSystem sc, RenderPasses renderPass);
        void CreateDeviceObjects(GraphicsDevice gd, CommandList cl, GraphicsSystem sc);
        void DestroyDeviceObjects();
        void Dispose();
        RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        RenderPasses RenderPasses();

        // public RenderPasses RenderPasses() { 
        //     return Glitch.Graphics.RenderPasses.Standard;
        // }
    }

}
