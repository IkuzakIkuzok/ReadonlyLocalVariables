
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
        public async Task Tuple()
        {
            var source = @"
using ReadonlyLocalVariables;
using System;

class C
{
    [ReassignableVariable(""z"")]
    void M()
    {
        var x = 0;
        var y = 0;
        var z = 0;
        ({|#0:x|}, {|#1:y|}, z) = (1, 2, 3);
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
    }
}
";

            var fixedSource = @"
using ReadonlyLocalVariables;
using System;

class C
{
    [ReassignableVariable(""z"")]
    void M()
    {
        var x = 0;
        var y = 0;
        var z = 0;
        (var x1, var y1, z) = (1, 2, 3);
        Console.WriteLine(x1);
        Console.WriteLine(y1);
        Console.WriteLine(z);
    }
}
";

            var expectedX = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("x")
                .WithLocation(0);
            var expectedY = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(1);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expectedX, expectedY);
        } // public async Task Tuple ()

        [TestMethod]
        public async Task TupleWithClassMembers()
        {
            var source = @"
using System;

class C
{
    int x;
    int y { get; set; }

    void M()
    {
        var y = 0;
        (x, {|#0:y|}, this.y, var w) = (1, 2, 3, 4);
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(this.y);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    int x;
    int y { get; set; }

    void M()
    {
        var y = 0;
        (x, var y1, this.y, var w) = (1, 2, 3, 4);
        Console.WriteLine(x);
        Console.WriteLine(y1);
        Console.WriteLine(this.y);
    }
}
";
            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task TupleWithClassMembers ()

        [TestMethod]
        public async Task AddAttributeForTuple()
        {
            var source = @"
class C
{
    int x;
    
    void M()
    {
        var y = 0;
        var z = 0;
        (x, {|#0:y|}, {|#1:z|}, var w) = (1, 2, 3, 4);
    }
}
";

            var fixedSource = @"using ReadonlyLocalVariables;

class C
{
    int x;

    [ReassignableVariable(""y"", ""z"")]
    void M()
    {
        var y = 0;
        var z = 0;
        (x, y, z, var w) = (1, 2, 3, 4);
    }
}
";

            var expectedY = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("y")
                .WithLocation(0);
            var expectedZ = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                .WithArguments("z")
                .WithLocation(1);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, ADD_ATTRIBUTE, expectedY, expectedZ);
        } // public async Task AddAttributeForTuple ()
    } // public sealed partial class CodeFixTest
} // namespace ReadonlyLocalVariables.Test
