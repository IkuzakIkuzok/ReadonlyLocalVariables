﻿
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verifier = ReadonlyLocalVariables.Test.Verifiers.AnalyzerVerifier<ReadonlyLocalVariables.ReadonlyLocalVariablesAnalyzer>;

namespace ReadonlyLocalVariables.Test
{
    [TestClass]
    public sealed class AnalyzerTest
    {
        private static readonly string diagnosticId = ReadonlyLocalVariablesAnalyzer.DiagnosticId;

        [TestMethod]
        public async Task NoDiagnostics()
        {
            var test = @"";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task NoDiagnostics ()

        [TestMethod]
        public async Task LocalReassignment()
        {
            var test = @"
class C
{
    void M()
    {
        var i = 0;
        {|#0:i = 1|};
    }
}
";
            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task LocalReassignment ()

        [TestMethod]
        public async Task CheckAttribute()
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
        } // public async Task CheckAttribute ()

        [TestMethod]
        public async Task IgnoreClassMember()
        {
            var test = @"
class C
{
    int m;
    int P { get; set; }

    void M()
    {
        m = 1;
        P = 1;
    }
}
";

            await Verifier.VerifyAnalyzerAsync(test);
        } // public async Task IgnoreClassMember ()

        [TestMethod]
        public async Task NameCollision()
        {
            var test = @"
class C
{
    int m;

    void M()
    {
        var m = 0;

        this.m = 1;
        {|#0:m = 1|};
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("m")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task NameCollision ()

        [TestMethod]
        public async Task CompoundAssignment()
        {
            var test = @"
class C
{
    void M()
    {
        var i = 0;

        {|#0:i += 1|};
        {|#1:i -= 1|};
        {|#2:i *= 1|};
        {|#3:i /= 1|};
        {|#4:i %= 1|};
        {|#5:i &= 1|};
        {|#6:i |= 1|};
        {|#7:i ^= 1|};
        {|#8:i <<= 1|};
        {|#9:i >>= 1|};

        var o = new object();
        {|#10:o ??= new object()|};
    }
}
";

            var expected = new List<DiagnosticResult>
            {
                new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error).WithArguments("o").WithLocation(10)
            };

            expected.AddRange(
                Enumerable.Range(0, 10).Select(index =>
                    new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                        .WithArguments("i")
                        .WithLocation(index)
                )
            );

            await Verifier.VerifyAnalyzerAsync(test, expected.ToArray());
        } // public async Task CompoundAssignment ()

        [TestMethod]
        public async Task ForStatement()
        {
            var test = @"
class C
{
    void M()
    {
        for (var i = 0; i < 10; i += 2)
        {
            {|#0:i += 1|};
        }
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("i")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task ForStatement ()

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

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
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

            var expectedX = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("x")
                .WithLocation(0);
            var expectedY = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
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

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
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

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(0);
            await Verifier.VerifyAnalyzerAsync(test, expected);
        } // public async Task CheckAttributeForTuple ()
    } // public sealed class AnalyzerTest
} // namespace ReadonlyLocalVariables.Test
