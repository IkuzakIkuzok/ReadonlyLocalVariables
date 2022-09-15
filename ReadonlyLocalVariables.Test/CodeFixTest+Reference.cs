
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
        public async Task UpdateReference()
        {
            var source = @"
using System;

class C
{
    void M()
    {
        var i = 0;
        Console.WriteLine(i);

        {|#0:i = 1|};
        Console.WriteLine(i);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    void M()
    {
        var i = 0;
        Console.WriteLine(i);

        var i1 = 1;
        Console.WriteLine(i1);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task UpdateReference ()

        [TestMethod]
        public async Task UpdateReferenceWithSameName()
        {
            var source = @"
using System;

class C
{
    int m;

    void M()
    {
        var m = 0;
        Console.WriteLine(this.m);
        Console.WriteLine(m);

        this.m = 1;
        {|#0:m = 1|};
        Console.WriteLine(this.m);
        Console.WriteLine(m);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    int m;

    void M()
    {
        var m = 0;
        Console.WriteLine(this.m);
        Console.WriteLine(m);

        this.m = 1;
        var m1 = 1;
        Console.WriteLine(this.m);
        Console.WriteLine(m1);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("m")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task UpdateReferenceWithSameName ()

        [TestMethod]
        public async Task UpdateReferenceInsideScope()
        {
            var source = @"
using System;

class C
{
    void M1()
    {
        var i = 0;

        {|#0:i = 1|};
        Console.WriteLine(i);
    }

    void M2()
    {
        var i = 1;
        Console.WriteLine(i);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    void M1()
    {
        var i = 0;

        var i1 = 1;
        Console.WriteLine(i1);
    }

    void M2()
    {
        var i = 1;
        Console.WriteLine(i);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task UpdateReferenceInsideScope ()

        [TestMethod]
        public async Task NewVariableInLocalFunction()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 0;

        void L()
        {
            {|#0:i = 1|};
        }
    }
}
";

            var fixedSource = @"
class C
{
    void M()
    {
        var i = 0;

        void L()
        {
            var i1 = 1;
        }
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task NewVariableInLocalFunction ()

        [TestMethod]
        public async Task AddAttributeToLocalFunction()
        {
            var source = @"
class C
{
    void M()
    {
        var i = 0;

        void L()
        {
            {|#0:i = 1|};
        }
    }
}
";

            var fixedSource = @"using ReadonlyLocalVariables;

class C
{
    void M()
    {
        var i = 0;

        [ReassignableVariable(""i"")]
        void L()
        {
            i = 1;
        }
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, ADD_ATTRIBUTE, expected);
        } // public async Task AddAttributeToLocalFunction ()

        [TestMethod]
        public async Task UpdateReferenceInLocalFunction()
        {
            // NOTE: Specifications for updating references after the closure may change in the future.

            var source = @"
using System;

class C
{
    void M()
    {
        var i = 0;

        void L()
        {
            {|#0:i = 1|};
            Console.WriteLine(i);
        }

        Console.WriteLine(i);
    }
}
";

            var fixedSource = @"
using System;

class C
{
    void M()
    {
        var i = 0;

        void L()
        {
            var i1 = 1;
            Console.WriteLine(i1);
        }

        Console.WriteLine(i);
    }
}
";

            var expected = new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error)
                            .WithArguments("i")
                            .WithLocation(0);
            await Verifier.VerifyCodeFixAsync(source, fixedSource, NEW_VARIABLE, expected);
        } // public async Task UpdateReferenceInLocalFunction ()
    } // public sealed partial class CodeFixTest
} // namespace ReadonlyLocalVariables.Test
