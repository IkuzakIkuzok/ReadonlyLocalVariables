
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
        public async Task Tuple()
        {
            var test = @"
class C
{
    void M()
    {
        var x = 0;
        var y = 0;
        ({|#0:x|}, {|#1:y|}) = (1, 2);
    }
}
";

            var expectedX = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                .WithArguments("x")
                .WithLocation(0);
            var expectedY = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(1);
            await Verifier.VerifyAnalyzerAsync(test, expectedX, expectedY);
        }  // public async Task Tuple ()

        [TestMethod]
        public async Task TupleOnRightSide()
        {
            var test = @"
class C
{
    void M()
    {
        var i = 0;
        var j = 1;
        (var x, var y) = (i, j);
    }
}
";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task TupleOnRightSide ()

        [TestMethod]
        public async Task TupleWithClassMember()
        {
            var test = @"
class C
{
    int x;
    int Z { get; set; }

    void M()
    {
        var y = 0;
        (x, {|#0:y|}, Z, var w) = (1, 2, 3, 4);
    }
}
";

            var expected = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task TupleWithClassMember ()

        [TestMethod]
        public async Task CheckAttributeForTuple()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    [ReassignableVariable(""x"")]
    void M()
    {
        var x = 0;
        var y = 0;
        (x, {|#0:y|}) = (1, 2);
    }
}
";

            var expected = new DiagnosticResult(ReassignmentId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task CheckAttributeForTuple ()
    } // public sealed partial class AnalyzerTest
} // namespace ReadonlyLocalVariables.Test
