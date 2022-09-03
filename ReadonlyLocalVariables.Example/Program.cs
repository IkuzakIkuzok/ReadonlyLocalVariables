
// (c) 2022 Kazuki KOHZUKI

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

        void F()
        {
            var i = 0;
            Console.WriteLine(i);
            i = 1;
            Console.WriteLine(i);

            normal = 1;
        }

        Console.WriteLine(normal);
    }

    void Foo()
    {
        var normal = 1;
        Console.WriteLine(normal);

        for (var i = 0; i < 10; i += 2)
        {
            i += i;
        }
    }
}
