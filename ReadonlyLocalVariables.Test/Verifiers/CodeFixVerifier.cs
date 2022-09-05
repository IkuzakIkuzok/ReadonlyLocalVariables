
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using System.Threading.Tasks;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    /// <summary>
    /// Provides verification of code fixes.
    /// </summary>
    /// <typeparam name="TAnalyzer">Analyzer.</typeparam>
    /// <typeparam name="TCodeFix">Code fix provider.</typeparam>
    internal class CodeFixVerifier<TAnalyzer, TCodeFix> where TAnalyzer : DiagnosticAnalyzer, new() where TCodeFix : CodeFixProvider, new()
    {
        /// <summary>
        /// Verifies the code fix specified by an index asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="codeActionIndex">The index of code action to apply.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, int codeActionIndex, params DiagnosticResult[] expected)
        {
            var test = new CodeFixTest<TAnalyzer, TCodeFix>()
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionIndex = codeActionIndex,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyCodeFixAsync (string, string, int, params DiagnosticResult[])

        /// <summary>
        /// Verifies the default code fix asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, 0, expected);

        /// <summary>
        /// Verifies the code fix specified by a equivalence key asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="equivalenceKey">The equivalence key of code action to apply.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey, params DiagnosticResult[] expected)
        {
            var test = new CodeFixTest<TAnalyzer, TCodeFix>()
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyCodeFixAsync (string, string, string, params DiagnosticResult[])
    } // internal class CodeFixVerifier<TAnalyzer, TCodeFix> where TAnalyzer : DiagnosticAnalyzer, new() where TCodeFix : CodeFixProvider, new()
} // namespace ReadonlyLocalVariables.Test.Verifiers
