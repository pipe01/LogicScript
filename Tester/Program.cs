using LogicScript;
using LogicScript.ByteCode;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Testing;
using LogicScript.Transpiling;
using System;
using System.IO;
using System.Linq;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            var (script, errors) = Script.Parse(File.ReadAllText("test.lsx"));

            foreach (var err in errors)
            {
                Console.WriteLine(err);
            }

            var program = new Transpiler().Transpile(script);
            Console.WriteLine(string.Join(", ", program));

            var machine = new MyMachine();
            var input = new bool[machine.InputCount];

            machine.ReadInputs(input);
            var n = new BitsValue(input);

            program(machine, n);

            // new CPU(program, new MyMachine()).Run(true);

            // if (script != null)
            // {
            //     var deleg = Compiler.Compile(script);
            //     deleg(new MyMachine(), true);
            // }

            // var (bench, benchErrors) = TestBench.Parse(File.ReadAllText("test.lsbench"), script);
        }

        class MyMachine : IUpdatableMachine
        {
            public int InputCount => 3;

            public int OutputCount => 2;

            private BitsValue[] Registers;

            public void Print(string msg)
            {
                Console.WriteLine(msg);
            }

            public void ReadInputs(Span<bool> values)
            {
                values[0] = true;
                values[1] = true;
                // for (int i = 0; i < values.Length; i++)
                // {
                //     values[i] = i % 2 == 0;
                // }
            }

            public bool ReadInput(int index)
            {
                return false;
            }

            public void WriteOutputs(int startIndex, Span<bool> value)
            {
                // Console.WriteLine(string.Join(' ', value.ToArray().Select(o => o ? "1" : "0").ToArray()));
            }

            public void WriteOutput(int index, bool value)
            {
                // Console.WriteLine(string.Join(' ', value.ToArray().Select(o => o ? "1" : "0").ToArray()));
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

            public void QueueUpdate()
            {
            }
        }
    }
}
