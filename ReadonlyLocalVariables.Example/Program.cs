
// (c) 2022 Kazuki KOHZUKI

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
