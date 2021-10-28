using LogicScript;
using LogicScript.Interpreting;
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
    $print ""Nice""
end

");

                new Interpreter(script).Run(new MyMachine());
            }
            catch (ParseException ex)
            {
                Console.WriteLine($"{ex.Message} at {ex.Location}");
                Console.ReadKey();
                return;
            }

            Console.ReadKey();
        }

        class MyMachine : IMachine
        {
            public int InputCount => 3;

            public int OutputCount => 2;

            public void Print(string msg)
            {
                Console.WriteLine(msg);
            }

            public void ReadInput(Span<bool> values)
            {
            }

            public void WriteOutput(Span<bool> values)
            {
            }
        }
    }
}
