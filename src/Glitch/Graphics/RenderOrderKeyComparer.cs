using System.Collections.Generic;
using System.Numerics;

namespace Glitch.Graphics
{
    public class RenderOrderKeyComparer : IComparer<IRenderable>
    {
        public Vector3 CameraPosition { get; set; }
        public int Compare(IRenderable x, IRenderable y)
        {
            return x.GetRenderOrderKey(CameraPosition).CompareTo(y.GetRenderOrderKey(CameraPosition));
        }
    }
}