
# ReadonlyLocalVariables

[![Test](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml)

Prohibits reassignment of local variables.

## Installation

You can download packages from [NuGet](https://www.nuget.org/packages/ReadonlyLocalVariables/).

## Usage

After installing the analyzer, reassignment to a local variable results in an error.
If there are local variables for which you want to allow reassignment in exceptional cases,
you can explicitly specify this by adding the `ReadonlyLocalVariables.ReassignableVariable` attribute to the method.


```C#
using ReadonlyLocalVariables;
using System;

class C
{
    private static int Field = 0;

    [ReassignableVariable("reassignable")]  // Explicitly state which normal variables are allowed to be reassigned.
    static void Main(string[] args)
    {
        var normal = 0;
        var reassignable = 0;

        Console.WriteLine(normal);
        Console.WriteLine(reassignable);
        Console.WriteLine(Field);

        normal = 1;        // Normal variables are treated as read-only.
        reassignable = 1;  // Specially marked local variables can be reassigned.
        Field = 1;         // Class members are not inspected because they have the `readonly` keyword.

        Console.WriteLine(normal);
        Console.WriteLine(reassignable);
        Console.WriteLine(Field);
    }
}
```

## Code Fix

The code fix function (implemented in v2.0.0) can correct a no-reassignment error in two ways.

To prevent reassignment of local variable, a new local variable declaration can be added.

```diff
var local = 0;
Console.WriteLine(local);

-local = 1;
-Console.WriteLine(local);
+var local1 = 1;
+Console.WriteLine(local1);
```

References below the new variable declaration are automatically updated.
Since the automatically generated identifier names are simplified,
it is recommended that they be refactored to appropriate names.

Alternatively, reassignment can be allowed by adding an attribute.

```diff
+using ReadonlyLocalVariables;

+[ReassignableVariable("local")]
void Func()
{
    var local = 0;
    Console.WriteLine(local);
    
    local = 1;
    Console.WriteLine(local);
}
```
