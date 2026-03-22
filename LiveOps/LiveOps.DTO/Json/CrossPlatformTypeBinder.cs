using System;
using Newtonsoft.Json.Serialization;

namespace Madbox.LiveOps.DTO.Json
{
    /// <summary>
    /// Maps JSON type names between Unity (mscorlib) and server (System.Private.CoreLib) runtimes.
    /// </summary>
    public sealed class CrossPlatformTypeBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                Type dtoType = Type.GetType($"{typeName}, Madbox.LiveOps.DTO", false);
                if (dtoType != null)
                {
                    return dtoType;
                }
            }

            string targetAssembly = assemblyName?.Replace("mscorlib", "System.Private.CoreLib");
            return Type.GetType($"{typeName}, {targetAssembly}", throwOnError: false);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            string currentAssembly = serializedType.Assembly.FullName;
            assemblyName = currentAssembly.Replace("System.Private.CoreLib", "mscorlib");
            typeName = serializedType.FullName;
        }
    }
}
