
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Verifier = ReadonlyLocalVariables.Test.Verifiers.AnalyzerVerifier<ReadonlyLocalVariables.ReadonlyLocalVariablesAnalyzer>;

namespace ReadonlyLocalVariables.Test
{
    public sealed partial class AnalyzerTest
    {
        [TestMethod]
        public async Task AssignToParameter()
        {
            var test = @"
class C
{
    void M(int i)
    {
        {|#0:i = 1|};
    }
}
";

            var expected = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task AssignToParameter ()

        [TestMethod]
        public async Task AssignToOutParameter()
        {
            var test = @"
class C
{
    void M(out int i)
    {
        {|#0:i = 1|};
    }
}
";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task AssignToOutParameter ()
    } // public sealed partial class AnalyzerTest
} // namespace ReadonlyLocalVariables.Test
