
// (c) 2022 Kazuki KOHZUKI

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Options = System.Collections.Generic.Dictionary<string, object>;
using Verifier = ReadonlyLocalVariables.Test.Verifiers.CodeFixVerifier<
                    ReadonlyLocalVariables.ReadonlyLocalVariablesAnalyzer,
                    ReadonlyLocalVariables.ReadonlyLocalVariablesCodeFixProvider
                 >;

namespace ReadonlyLocalVariables.Test
{
    [TestClass]
    public sealed partial class CodeFixTest
    {
        // Code action equivalence keys
        private const string NEW_VARIABLE = "NewVariable";
        private const string ADD_ATTRIBUTE = "AddAttribute";

        private static readonly string diagnosticId = ReadonlyLocalVariablesAnalyzer.DiagnosticId;

        [TestMethod]
        public async Task NewVariable()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 0;

        {|#0:i = 1|};
    }
}
";

            var fixedSource = @"
class C
{
    void M()
    {
        var i = 0;

        var i1 = 1;
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task NewVariable ()

        [TestMethod]
        public async Task AddAttribute()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 0;

        {|#0:i = 1|};
    }
}
";

            var fixedSource = @"using ReadonlyLocalVariables;

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

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, ADD_ATTRIBUTE, expected);
        } // public async Task AddAttribute ()

        [TestMethod]
        public async Task ArgumentWithOut()
        {
            var source = @"
using System;

class C
{
    int i = 0;

    void M()
    {
        var i = 0;
        int.TryParse(""1"", {|#0:out i|});
        Console.WriteLine(i);
        Console.WriteLine(this.i);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    int i = 0;

    void M()
    {
        var i = 0;
        int.TryParse(""1"", out var i1);
        Console.WriteLine(i1);
        Console.WriteLine(this.i);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task ArgumentWithOut ()

        [TestMethod]
        public async Task AddAttributeForArgumentWithOut()
        {
            var source = @"
using ReadonlyLocalVariables;

class C
{
    void M()
    {
        var i = 0;
        int.TryParse(""1"", {|#0:out i|});
    }
}
";

            var fixedSource = @"
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

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, ADD_ATTRIBUTE, expected);
        } // public async Task AddAttributeForArgumentWithOut ()

        [TestMethod]
        public async Task TopLevelStatement()
        {
            var source = @"
var i = 0;
{|#0:i = 1|};
";

            var fixedSource = @"
var i = 0;
var i1 = 1;
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, new Options() { { "OutputKind", OutputKind.ConsoleApplication } }, expected);
        } // public async Task TopLevelStatement ()
    } // public sealed partial class CodeFixTest
} // namespace ReadonlyLocalVariables.Test
