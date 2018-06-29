using System.Reflection;
using System.Runtime.Loader;

namespace Glitch.ProjectSystem
{
    public class EngineLoadContext : AssemblyLoadContext
    {
        public EngineLoadContext()
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return Assembly.Load(assemblyName);
        }
    }
}
