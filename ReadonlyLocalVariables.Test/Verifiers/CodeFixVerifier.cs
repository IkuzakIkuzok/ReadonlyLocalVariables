
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using System.Threading.Tasks;
using Options = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>;

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
        /// Verifies the default code fix asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, 0, expected);

        /// <summary>
        /// Verifies the code fix specified by an index asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="codeActionIndex">The index of code action to apply.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, int codeActionIndex, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, codeActionIndex, 1, expected);

        /// <summary>
        /// Verifies the code fixes specified by an index asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="codeActionIndex">The index of code action to apply.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <param name="numberOfFixAllIterations">The number of code fix iterations expected.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, int codeActionIndex, int numberOfFixAllIterations, params DiagnosticResult[] expected)
        {
            var test = new CodeFixTest<TAnalyzer, TCodeFix>()
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionIndex = codeActionIndex,
                NumberOfFixAllIterations = numberOfFixAllIterations,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyCodeFixAsync (string, string, int, int, params DiagnosticResult[])

        /// <summary>
        /// Verifies the code fix specified by a equivalence key asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="equivalenceKey">The equivalence key of code action to apply.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, equivalenceKey, 1, expected);

        /// <summary>
        /// Verifies the code fixes specified by a equivalence key asynchronously.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="equivalenceKey">The equivalence key of code action to apply.</param>
        /// <param name="numberOfFixAllIterations">The number of code fix iterations expected.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey, int numberOfFixAllIterations, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, equivalenceKey, numberOfFixAllIterations, default, expected);

        /// <summary>
        /// Verifies the code fix specified by a equivalence key asynchronously using the specified compilation options.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="equivalenceKey">The equivalence key of code action to apply.</param>
        /// <param name="compilationOptions">The compilation options.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey, Options? compilationOptions, params DiagnosticResult[] expected)
            => await VerifyCodeFixAsync(source, fixedSource, equivalenceKey, 1, compilationOptions, expected);

        /// <summary>
        /// Verifies the code fixes specified by a equivalence key asynchronously using the specified compilation options.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="fixedSource">Fixed souce code.</param>
        /// <param name="equivalenceKey">The equivalence key of code action to apply.</param>
        /// <param name="numberOfFixAllIterations">The number of code fix iterations expected.</param>
        /// <param name="compilationOptions">The compilation options.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey, int numberOfFixAllIterations, Options? compilationOptions, params DiagnosticResult[] expected)
        {
            var test = new CodeFixTest<TAnalyzer, TCodeFix>(compilationOptions)
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
                NumberOfFixAllIterations = numberOfFixAllIterations,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyCodeFixAsync(string, string, string, int, Options?, params DiagnosticResult[])
    } // internal class CodeFixVerifier<TAnalyzer, TCodeFix> where TAnalyzer : DiagnosticAnalyzer, new() where TCodeFix : CodeFixProvider, new()
} // namespace ReadonlyLocalVariables.Test.Verifiers
