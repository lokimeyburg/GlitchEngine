using System;
using System.IO;

namespace Glitch.Assets
{

    public class DirectoryNode
    {
        public string FullPath { get; }
        public string FolderName { get; set; }
        public AssetInfo[] AssetInfos { get; }
        public DirectoryNode[] Children { get; }

        public DirectoryNode(string path, AssetInfo[] assetInfos, DirectoryNode[] children)
        {
            FullPath = path;
            FolderName = new DirectoryInfo(path).Name;
            AssetInfos = assetInfos;
            Children = children;
        }
    }
}