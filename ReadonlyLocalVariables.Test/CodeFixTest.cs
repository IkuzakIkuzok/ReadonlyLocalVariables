
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
    [TestClass]
    public sealed class CodeFixTest
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
    } // public sealed class CodeFixTest
} // namespace ReadonlyLocalVariables.Test
