
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Threading;
using System.Threading.Tasks;
using Options = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>;

namespace ReadonlyLocalVariables.Test.Verifiers
{
    /// <summary>
    /// Provide verification of code analyzer.
    /// </summary>
    /// <typeparam name="TAnalyzer"></typeparam>
    internal class AnalyzerVerifier<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        /// <summary>
        /// Verifies the code analysis asynchronously.
        /// </summary>
        /// <param name="source">Input souce code.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new AnalyzerTest<TAnalyzer>()
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyAnalyzerAsync (string, params DiagnosticResult[])

        /// <summary>
        /// Verifies the code analysis asynchronously using the specified compilation options.
        /// </summary>
        /// <param name="source">Input source code.</param>
        /// <param name="compilationOptions">The compilation options.</param>
        /// <param name="expected">The list of diagnostics expected in the <paramref name="source"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal static async Task VerifyAnalyzerAsync(string source, Options compilationOptions, params DiagnosticResult[] expected)
        {
            var test = new AnalyzerTest<TAnalyzer>(compilationOptions)
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        } // internal static async Task VerifyAnalyzerAsync(string, Options, params DiagnosticResult[])
    } // internal class AnalyzerVerifier<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
} // namespace ReadonlyLocalVariables.Test.Verifiers
