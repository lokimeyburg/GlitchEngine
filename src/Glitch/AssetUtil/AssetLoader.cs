using System;
using System.IO;

namespace Glitch.AssetUtil
{
    public interface AssetLoader
    {
        string FileExtension { get; }
        object Load(Stream s);
    }

    public interface AssetLoader<out TAsset> : AssetLoader
    {
        new TAsset Load(Stream s);
    }

    public abstract class ConcreteLoader<T> : AssetLoader<T>
    {
        public abstract string FileExtension { get; }

        public abstract T Load(Stream s);

        object AssetLoader.Load(Stream s) => Load(s);
    }
}