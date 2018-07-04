using System.Collections.Generic;
using Glitch.Assets;

namespace Glitch.ProjectSystem
{
    public class ProjectManifest
    {
        public string Name { get; set; }
        public string AssetRoot { get; set; } = "Assets";
        public List<string> ManagedAssemblies { get; set; } = new List<string>();
        public AssetID OpeningScene { get; set; }

        public List<StartupFunction> GameStartupFunctions { get; set; } = new List<StartupFunction>();
        public string GraphicsPreferencesProviderTypeName { get; set; }

        public ProjectManifest()
        {
        }
    }

    public class StartupFunction
    {
        public string TypeName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
    }
}