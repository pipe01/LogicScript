using LogicScript;
using LogicScript.Data;
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
    on = asd
    $print on
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

            private BitsValue[] Registers;

            public void Print(string msg)
            {
                Console.WriteLine(msg);
            }

            public void ReadInput(Span<bool> values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = i % 2 == 0;
                }
            }

            public void WriteOutput(int index, bool value)
            {
            }

            public void AllocateRegisters(int count)
            {
                Registers = new BitsValue[count];
            }

            public BitsValue ReadRegister(int index)
            {
                return Registers[index];
            }

            public void WriteRegister(int index, BitsValue value)
            {
                Registers[index] = value;
            }
        }
    }
}
