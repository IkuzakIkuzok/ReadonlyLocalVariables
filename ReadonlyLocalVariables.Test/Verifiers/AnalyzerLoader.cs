
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    internal class AnalyzerLoader : IAnalyzerAssemblyLoader
    {
        private readonly object lockObject = new();

        private readonly Dictionary<string, Assembly> loadedAssemblies = new();

        public Assembly LoadFromPath(string fullPath)
        {
            lock (lockObject)
            {
                if (this.loadedAssemblies.TryGetValue(fullPath, out var assembly))
                    return assembly;
            }

            var asm = Assembly.LoadFrom(fullPath);

            lock (lockObject)
            {
                loadedAssemblies[fullPath] = asm;
            }

            return asm;
        } // public Assembly LoadFromPath (string)

        public void AddDependencyLocation(string fullPath) { }
    } // internal class AnalyzerLoader : IAnalyzerAssemblyLoader
} // namespace ReadonlyLocalVariables.Test.Verifiers
