
// (c) 2022 Kazuki KOHZUKI

using ReadonlyLocalVariables;
using System;

class C
{
    private static int Field = 0;

    [ReassignableVariable("Local")]  // Explicitly state which local variables are allowed to be reassigned.
    static void Main(string[] args)
    {
        var local = 0;
        var Local = 0;

        Console.WriteLine(local);
        Console.WriteLine(Local);
        Console.WriteLine(Field);

        local = 1;  // Local variables are treated as read-only.
        Local = 1;  // Local variable allowed to be reassigned.
        Field = 1;  // Class members are not inspected because they have the `readonly` keyword.

        Console.WriteLine(local);
        Console.WriteLine(Local);
        Console.WriteLine(Field);
    }
}
