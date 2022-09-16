
# ReadonlyLocalVariables

[![Test](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml/badge.svg)](https://github.com/IkuzakIkuzok/ReadonlyLocalVariables/actions/workflows/dotnet.yml)

Prohibits reassignment of local variables.

## Installation

You can download the package from [NuGet](https://www.nuget.org/packages/ReadonlyLocalVariables/).

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

    [ReassignableVariable("reassignable")]  // Explicitly state which local variables are allowed to be reassigned.
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

Multiple identifier names can be specified in one attribute.

```C#
[ReassignableVariable("i", "j")]
void M()
{
    var i = 0;
    var j = 1;
}
```

### Parameter

Values received as parameters cannot be reassigned, just like local variables.
However, this does not apply to parameters with the `out` parameter modifier, since the value must be set before returning.

### Argument with `out`

Passing an already declared local variable with the `out` parameter modifier is also prohibited.

```C#
var i = 0;
if (int.TryParse("1", out i))  // Error
    Console.WriteLine(i);
```

To avoid this error, use variable declarations with `out var`.

```C#
if (int.TryParse("1", out var i))
    Console.WriteLine(i);
```

(Permission by attribute is also possible, although not recommended.)

### Tuple

Assignments to tuples containing predefined local variables also result in an error.

```C#
var x = 0;
var y = 0;
(x, y) = (1, 2);  // Error
```

Use declarations inside tuples instead.

```C#
(var x, var y) = (1, 2);
```

### For Statement

Reassignment of local variables is not inspected in the control of `for` statements.

```C#
for (var i = 0; i < 10; i += 2)  // OK
{
    i -= 1;  // Error
}
```

### Compound Assignment

Compound assignments are also prohibited because they are assignment operations.

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

Code fix for tuples or arguments with out parameter modifiers can be done in the same way.

### Compound Assignment

Compound assignments are also modified with the appropriate formulas.

```diff
var i = 0;

-i += 1;
+var i1 = i + 1;
```

# Misc.

## `CS8032`

`CS8032` may occur when building a project using this analyzer.
The following change to csproj resolves this warning.

```diff
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="ReadonlyLocalVariables" Version="2.5.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
+   <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="4.3.0">
+     <PrivateAssets>all</PrivateAssets>
+     <IncludeAssets>runtime; build; native; contentfiles; analyzers;</IncludeAssets>
+   </PackageReference>
  </ItemGroup>

</Project>
```
