using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using System;
using System.IO;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var script = Script.Parse(File.ReadAllText("test.lsx"));

                script.Run(new MyMachine());
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

            public void WriteOutput(int startIndex, Span<bool> value)
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
