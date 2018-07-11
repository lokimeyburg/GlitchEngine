using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Glitch.ProjectSystem
{
    public class EngineSerializationBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Assembly> _projectLoadedAssemblies = new Dictionary<string, Assembly>();
        private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder();

        public void ClearAssemblies() => _projectLoadedAssemblies.Clear();

        public void AddProjectAssembly(Assembly assembly)
        {
            _projectLoadedAssemblies.Add(assembly.GetName().Name, assembly);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            _defaultBinder.BindToName(serializedType, out assemblyName, out typeName);
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            Assembly assembly;
            if (_projectLoadedAssemblies.TryGetValue(assemblyName, out assembly))
            {
                return assembly.GetType(typeName);
            }
            else
            {
                return _defaultBinder.BindToType(assemblyName, typeName);
            }
        }
    }
}
