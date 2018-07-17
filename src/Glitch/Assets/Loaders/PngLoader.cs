using System.IO;
using SixLabors.ImageSharp;
using Veldrid.ImageSharp;
using System;

namespace Glitch.Assets
{
    // public class PngLoader : ConcreteLoader<ImageSharpMipmapChain>
    // {
    //     public override string FileExtension => "png";

    //     public override ImageSharpMipmapChain Load(Stream s)
    //     {
    //         return new ImageSharpMipmapChain(Image.Load(s));
    //     }
    // }

    public class ImageSharpTextureLoader : ConcreteLoader<ImageSharpTexture>
    {
        public override string FileExtension => "png";

        public override ImageSharpTexture Load(Stream s)
        {
            // return new ImageSharpTexture(s.);
            return new ImageSharpTexture(Image.Load(s));
        }
    }
}