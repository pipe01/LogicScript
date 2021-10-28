using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript;
using LogicScript.Parsing;
using LogicScript.Parsing.Visitors;
using System;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var script = Script.Parse(@"input asd
input test
output'2 out

reg'123 on

when *
    on = test

    if test == 1
        on = 1
    else if test == 2
        on = 2
    else
        on = 3
    end
end

when *
    on = test
end

");
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"{ex.Message} at {ex.Location}");
                Console.ReadKey();
            }
        }
    }
}
