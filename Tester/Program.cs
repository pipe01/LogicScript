using LogicScript;
using LogicScript.Parsing;
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

const myconst = 123

reg'64 on

when *
    on = myconst

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
