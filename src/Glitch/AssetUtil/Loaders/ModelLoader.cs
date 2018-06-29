using System;
using System.IO;
using Veldrid;
using Veldrid.Utilities;

namespace Glitch.AssetUtil
{
    public class ObjFileLoader : ConcreteLoader<ObjFile>
    {
        public override string FileExtension => "obj";

        public override ObjFile Load(Stream s)
        {
            ObjParser parser = new ObjParser();
            return new ObjParser().Parse(s);
        }
    }

    public class FirstMeshObjLoader : ConcreteLoader<ConstructedMeshInfo>
    {
        public override string FileExtension => "obj";

        public override ConstructedMeshInfo Load(Stream s)
        {
             var objFile = new ObjParser().Parse(s);
             return objFile.GetFirstMesh();
        }
    }
}