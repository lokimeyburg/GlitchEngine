using System;

namespace Glitch.Assets 
{
    public class AssetInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public AssetInfo(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}