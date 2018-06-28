using System;
using System.Numerics;
using Veldrid.Utilities;
using Veldrid;

namespace Glitch.Graphics
{
    public interface ICullRenderable : IRenderable
    {
        bool Cull(ref BoundingFrustum visibleFrustum);

        // public bool Cull(ref BoundingFrustum visibleFrustum)
        // {
        //     return visibleFrustum.Contains(BoundingBox) == ContainmentType.Disjoint;
        // }

        BoundingBox BoundingBox();

        // public abstract BoundingBox BoundingBox { get; }

    }
}