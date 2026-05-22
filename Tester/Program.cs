using LogicScript;
using LogicScript.Data;
using LogicScript.Compiling;
using System;
using System.IO;
using System.Linq;
using LogicScript.Parsing;
using System.Collections.Generic;
using LogicScript.Parsing.Structures;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            var filename = args.Length > 0 ? args[0] : "test.lsx";

            var (script, errors) = Script.Parse(File.ReadAllText(filename), filename);

            foreach (var err in errors)
            {
                Console.WriteLine(err);
            }
            if (errors.Count > 0)
                return;

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

        class MyMachine : IMachine
        {
            public int InputCount { get; set; }

            public int OutputCount { get; set; }

            private ulong[] Registers;

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
                Console.WriteLine(value.ToStringBinary());
            }

            public void WriteOutput(int index, bool value)
            {
                Console.WriteLine($"Index {index} = {(value ? '1' : '0')}");
            }

            public void AllocateRegisters(int count)
            {
                Registers = new ulong[count];
            }

            public ulong ReadRegister(int index)
            {
                return Registers[index];
            }

            public void WriteRegister(int index, ulong value)
            {
                Registers[index] = value;
            }

            public void QueueUpdate()
            {
            }
        }

        class MyDebugger : IDebugger
        {
            public void TraceStatement(SourceSpan span, IDictionary<LocalInfo, ulong> locals)
            {
                Console.WriteLine(span);
            }
        }
    }
}
