#nullable disable

using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LogicScript;
using LogicScript.ByteCode;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Transpiling;

namespace LogicScript.Benchmarks
{
    [MemoryDiagnoser]
    public class LogicScriptBenchmark
    {
        private static readonly string[] Sources = [
            @"input a
input b
output c

when 1
    c = a & b
end
",
            @"when 1
    local $a'64 = 23947;
    local $b'64 = 12398;
    local $c'64 = 34598;

    $a = $b & $c
    $a = $b | $c
    $a = $b ^ $c
    $a = $b << $c
    $a = $b >> $c

    $a '= $b + $c
    $a '= $b * $c
    $a = $b / $c
    $a = $b ** $c
    $a = $b % $c

    $a = $b == $c
    $a = $b != $c
    $a = $b > $c
    $a = $b < $c
    $a = $b < $c

    $a = !$b
    $a = len($b)
    $a = allOnes($b)
end
"
        ];

        [Params(2)]
        public int Inputs { get; set; }
        [Params(1)]
        public int Outputs { get; set; }
        [Params(0)]
        public int SourceIndex { get; set; }

        private IMachine Machine;
        private Script Script;
        private CPU CPU;
        private TranspiledScript TranspiledScript;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var (script, errors) = Script.Parse(Sources[SourceIndex]);
            if (errors != null && errors.Count > 0)
            {
                Console.WriteLine("Found errors while parsing source:");

                foreach (var err in errors)
                {
                    Console.WriteLine("  " + err);
                }

                Environment.Exit(1);
                return;
            }

            this.Script = script!;
            this.Machine = new DummyMachine(Inputs, Outputs);

            System.Console.WriteLine($"asdasd {Inputs} {Outputs}");

            var bytecode = Compiler.Compile(script!);
            this.CPU = new CPU(bytecode, Machine);

            this.TranspiledScript = new Transpiler().Transpile(script!);
        }

        // [Benchmark]
        // public void RunInterpreted()
        // {
        //     Interpreter.Run(Script, Machine, false);
        // }

        // [Benchmark]
        // public void RunBytecode()
        // {
        //     CPU.Run(true);
        // }

        [Benchmark]
        public void RunTranspiled()
        {
            Span<bool> input = stackalloc bool[Machine.InputCount];
            Machine.ReadInputs(input);

            TranspiledScript(Machine, default);
        }

        [Benchmark(Baseline = true)]
        public void RunRaw()
        {
            Span<bool> input = stackalloc bool[Machine.InputCount];
            Machine.ReadInputs(input);

            Machine.WriteOutputs(0, [input[0] && input[1]]);
        }
    }

    class DummyMachine(int inputCount, int outputCount) : IUpdatableMachine
    {
        public int InputCount { get; } = inputCount;
        public int OutputCount { get; } = outputCount;

        private readonly bool[] Inputs = new bool[inputCount];
        private readonly bool[] Outputs = new bool[outputCount];

        private BitsValue[] Registers = [];

        public void Print(string msg)
        {
        }

        public void ReadInputs(Span<bool> values)
        {
            Inputs.CopyTo(values);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public void WriteOutputs(int startIndex, Span<bool> value)
        {
            value.CopyTo(Outputs.AsSpan()[startIndex..]);
        }

        public void WriteOutput(int index, bool value)
        {
            Outputs[index] = value;
        }

        public void AllocateRegisters(int count)
        {
            Array.Resize(ref Registers, count);
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

    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<LogicScriptBenchmark>();
        }
    }
}