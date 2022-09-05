
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    internal class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        private static readonly string AnalyzerPath = typeof(TAnalyzer).Assembly.Location;

        internal AnalyzerTest()
        {
            this.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project == null) return solution;
                project = project.AddAnalyzerReference(new AnalyzerFileReference(AnalyzerPath, new AnalyzerLoader()));
                return project.Solution;
            });
        } // ctor ()
    } // internal class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier> where TAnalyzer : DiagnosticAnalyzer, new()
} // namespace ReadonlyLocalVariables.Test.Verifiers
