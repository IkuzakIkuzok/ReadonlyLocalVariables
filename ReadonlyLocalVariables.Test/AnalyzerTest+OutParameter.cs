
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
        public async Task OutParameter()
        {
            var test = @"
class C
{
    int i;

    void M()
    {
        var i = 0;
        int.TryParse(""1"", {|#0:out i|});
        int.TryParse(""1"", out this.i);
        int.TryParse(""i"", out var j);
    }
}
";

            var expected = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                .WithArguments("i")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task OutParameter ()

        [TestMethod]
        public async Task CheckAttributeForOutParameter()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    [ReassignableVariable(""i"")]
    void M()
    {
        var i = 0;
        int.TryParse(""1"", out i);
    }
}
";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task CheckAttributeForOutParameter ()
    } // public sealed partial class AnalyzerTest
} // namespace ReadonlyLocalVariables.Test
