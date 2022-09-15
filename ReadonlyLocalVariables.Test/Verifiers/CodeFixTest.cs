
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Options = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    internal class CodeFixTest<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        private static readonly string AnalyzerPath = typeof(TAnalyzer).Assembly.Location;

        internal CodeFixTest()
        {
            this.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project == null) return solution;
                project = project.AddAnalyzerReference(new AnalyzerFileReference(AnalyzerPath, new AnalyzerLoader()));
                return project.Solution;
            });
        } // ctor ()

        internal CodeFixTest(Options? compilationOptions) : this()
        {
            this.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project == null) return solution;
                project = project.WithCompilationOptions(compilationOptions);
                return project.Solution;
            });
        } // ctor (Options?)
    } // internal class CodeFixTest<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, MSTestVerifier>
}
