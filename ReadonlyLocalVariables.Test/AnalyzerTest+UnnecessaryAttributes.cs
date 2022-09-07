
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Verifier = ReadonlyLocalVariables.Test.Verifiers.AnalyzerVerifier<ReadonlyLocalVariables.UnnecessaryAttributesDetector>;

namespace ReadonlyLocalVariables.Test
{
    public sealed partial class AnalyzerTest
    {
        private static readonly string UnnecessaryAttributesId = UnnecessaryAttributesDetector.AttributeDiagnosticId;
        private static readonly string UnnecessaryPermissionsId = UnnecessaryAttributesDetector.PermissionDiagnosticId;

        [TestMethod]
        public async Task NoInformation()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    [ReassignableVariable(""i"")]
    void M()
    {
        var i = 0;
        i = 1;
    }
}
";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task NoInformation ()

        [TestMethod]
        public async Task UnnecessaryPermission()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    [ReassignableVariable(""i"", {|#0:""j""|}, {|#1:""k""|})]
    void M()
    {
        var i = 0;
        var j = 0;
        i = 1;
    }
}
";

            var expectedJ = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("j")
                            .WithLocation(0);
            var expectedK = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("k")
                            .WithLocation(1);

            await Verifier.VerifyAnalyzerAsync(test, expectedJ, expectedK);
        } // public async Task UnnecessaryPermission ()

        [TestMethod]
        public async Task UnnecessaryAttribute()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    int i = 0;

    [{|#0:ReassignableVariable(""i"", ""j"", ""k"")|}]
    void M()
    {
        var j = 0;
        i = 1;
    }
}
";

            var expected = new DiagnosticResult(UnnecessaryAttributesId, DiagnosticSeverity.Info)
                            .WithArguments("ReassignableVariable")
                            .WithLocation(0);

            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task UnnecessaryAttribute ()

        [TestMethod]
        public async Task UnnecessaryAttributeForTuple()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    int i = 0;

    [ReassignableVariable({|#0:""i""|}, ""j"", {|#1:""k""|})]
    void M()
    {
        var j = 0;
        (i, j, var k) = (1, 2, 3);
    }
}
";

            var expectedI = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("i")
                            .WithLocation(0);
            var expectedK = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("k")
                            .WithLocation(1);

            await Verifier.VerifyAnalyzerAsync(test, expectedI, expectedK);
        } // public async Task UnnecessaryAttributeForTuple ()

        [TestMethod]
        public async Task UnnecessaryAttributeForOutParameter()
        {
            var test = @"
using ReadonlyLocalVariables;

class C
{
    int i = 0;

    [ReassignableVariable({|#0:""i""|}, ""j"", {|#1:""k""|})]
    void M()
    {
        var j = 0;
        int.TryParse(""1"", out i);
        int.TryParse(""2"", out j);
        int.TryParse(""3"", out var k);
    }
}
";

            var expectedI = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("i")
                            .WithLocation(0);
            var expectedK = new DiagnosticResult(UnnecessaryPermissionsId, DiagnosticSeverity.Info)
                            .WithArguments("k")
                            .WithLocation(1);

            await Verifier.VerifyAnalyzerAsync(test, expectedI, expectedK);
        } // public async Task UnnecessaryAttributeForOutParameter ()
    } // public sealed partial class AnalyzerTest
} // namespace ReadonlyLocalVariables.Test
