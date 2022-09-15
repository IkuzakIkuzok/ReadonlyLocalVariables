
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    internal static class ProjectOptionsHelper
    {
        private static readonly Dictionary<string, MethodInfo?> setters = new();

        internal static Project WithCompilationOption(this Project project, string name, object value)
        {
            var setterName = $"With{name}";
            if (!setters.TryGetValue(setterName, out var setter))
                setter = setters[setterName] = typeof(CompilationOptions).GetMethod(setterName);

            return project.WithCompilationOptions((CompilationOptions)setter?.Invoke(project.CompilationOptions, new[] { value })!);
        } // internal static Project WithCompilationOption (this Project, string, object)

        internal static Project WithCompilationOptions(this Project project, IEnumerable<KeyValuePair<string, object>>? kwargs)
        {
            if (kwargs == null) return project;
            foreach (var kwarg in kwargs)
                project = project.WithCompilationOption(kwarg.Key, kwarg.Value);
            return project;
        } // internal static Project WithCompilationOptions (this Project, IEnumerable<KeyValuePair<string, object>>?)
    } // internal static class ProjectOptionsHelper
} // namespace ReadonlyLocalVariables.Test.Verifiers
