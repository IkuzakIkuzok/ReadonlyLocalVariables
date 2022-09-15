
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Verifier = ReadonlyLocalVariables.Test.Verifiers.CodeFixVerifier<
                    ReadonlyLocalVariables.ReadonlyLocalVariablesAnalyzer,
                    ReadonlyLocalVariables.ReadonlyLocalVariablesCodeFixProvider
                 >;

namespace ReadonlyLocalVariables.Test
{
    public sealed partial class CodeFixTest
    {
        [TestMethod]
        public async Task CompoundAssignment()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 1;
        {|#0:i += 1|};
    }
}
";

            var fixedSource = @"
class C
{
    void M()
    {
        var i = 1;
        var i1 = i + 1;
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task CompoundAssignment ()

        [TestMethod]
        public async Task BooleanOperation()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 0;
        var b = true;

        {|#0:i &= 1|};
        {|#1:i |= 1|};

        {|#2:b &= true|};
        {|#3:b |= true|};
    }
}
";

            var fixedSource = @"
class C
{
    void M()
    {
        var i = 0;
        var b = true;

        var i1 = i & 1;
        var i2 = i1 | 1;

        var b1 = b && true;
        var b2 = b1 || true;
    }
}
";

            var expectedI0 = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            var expectedI1 = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(1);
            var expectedB0 = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("b")
                            .WithLocation(2);
            var expectedB1 = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("b")
                            .WithLocation(3);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, 2, expectedI0, expectedI1, expectedB0, expectedB1);
        } // public async Task BooleanOperation ()

        [TestMethod]
        public async Task CompoundAssignmentWithSameIdentifier()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 1;
        {|#0:i *= i|};
    }
}
";

            var fixedSource = @"
class C
{
    void M()
    {
        var i = 1;
        var i1 = i * i;
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task CompoundAssignmentWithSameIdentifier ()

        [TestMethod]
        public async Task CompoundAssignmentWithClassMember()
        {
            var source = @"
using System;

class C
{
    int m = 2;

    void M()
    {
        var m = 4;
        {|#0:m /= this.m|};
        Console.WriteLine(m);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    int m = 2;

    void M()
    {
        var m = 4;
        var m1 = m / this.m;
        Console.WriteLine(m1);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("m")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task CompoundAssignmentWithClassMember ()
    } // public sealed partial class CodeFixTest
} // namespace ReadonlyLocalVariables.Test
