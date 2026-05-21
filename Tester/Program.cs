using LogicScript;
using LogicScript.Data;
using LogicScript.Compiling;
using System;
using System.IO;
using System.Linq;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            var (script, errors) = Script.Parse(File.ReadAllText("test4.lsx"));

            foreach (var err in errors)
            {
                Console.WriteLine(err);
            }

            var program = Compiler.Compile(script);
            Console.WriteLine(string.Join(", ", program));

            var machine = new MyMachine
            {
                InputCount = script.Inputs.Values.Sum(o => o.BitSize),
                OutputCount = script.Outputs.Values.Sum(o => o.BitSize)
            };

            var scratch = new bool[Math.Max(machine.InputCount, machine.OutputCount)];

            program(machine, scratch, true);

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
            public int InputCount { get; set; }

            public int OutputCount { get; set; }

            private BitsValue[] Registers;

            public void Print(string msg)
            {
                Console.WriteLine(msg);
            }

            public BitsValue ReadInputs()
            {
                return new BitsValue(3, InputCount);
            }

            public bool ReadInput(int index)
            {
                Console.WriteLine($"Read input {index}");
                return false;
            }

            public void WriteOutputs(int startIndex, BitsValue value)
            {
                // Console.WriteLine(string.Join(' ', value.ToArray().Select(o => o ? "1" : "0").ToArray()));
            }

            public void WriteOutput(int index, bool value)
            {
                Console.WriteLine($"Index {index} = {(value ? '1' : '0')}");
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
