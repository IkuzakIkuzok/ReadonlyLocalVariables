
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;

namespace ReadonlyLocalVariables
{
    [Generator]
    public class ReassignableVariableAttributeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        } // public void Initialize (GeneratorInitializationContext)

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource(
                "__MutableVariablesRuleAttribute.cs",
                @"
using System;
namespace ReadonlyLocalVariables
{
    /// <summary>
    /// Specifies the name of identifier that is allowed to be reassigned within a scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class ReassignableVariableAttribute : Attribute
    {
        /// <summary>
        /// Gets the variable identifiers.
        /// </summary>
        internal string[] Identifiers { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref=""ReassignableVariableAttribute""/> class
        /// with the specified names. 
        /// </summary>
        /// <param name=""name"">The variable names.</param>
        public ReassignableVariableAttribute(params string[] names)
        {
            this.Identifiers = names;
        } // ctor (string)
    } // internal sealed class ReassignableVariableAttribute : Attribute
} // namespace ReadonlyLocalVariables
"
            );
        } // public void Execute (GeneratorExecutionContext)
    } // public class ReassignableVariableAttributeGenerator : ISourceGenerator
} // namespace ReadonlyLocalVariables
