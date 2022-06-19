using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LogicScript;
using LogicScript.ByteCode;
using LogicScript.Data;
using LogicScript.Interpreting;

namespace LogicScript.Benchmarks
{
    [MemoryDiagnoser]
    public class LogicScriptBenchmark
    {
        private readonly string Source =
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
";

        private readonly IMachine Machine = new DummyMachine();
        private readonly Script Script;
        private readonly CPU CPU;

        public LogicScriptBenchmark()
        {
            var (script, errors) = Script.Parse(Source);
            if (errors != null && errors.Count > 0)
            {
                Console.WriteLine("Found errors while parsing source:");

                foreach (var err in errors)
                {
                    System.Console.WriteLine("  " + err);
                }

                Environment.Exit(1);
                return;
            }

            this.Script = script!;
            
            var bytecode = LogicScript.ByteCode.Compiler.Compile(script!);
            this.CPU = new CPU(bytecode, Machine);
        }

        // [Benchmark]
        // public void RunInterpreted()
        // {
        //     Interpreter.Run(Script, Machine, false);
        // }

        [Benchmark]
        public void RunBytecode()
        {
            CPU.Run(true);
        }
    }

    class DummyMachine : IUpdatableMachine
    {
        public int InputCount => 0;

        public int OutputCount => 0;

        private BitsValue[] Registers = Array.Empty<BitsValue>();

        public void Print(string msg)
        {
        }

        public void ReadInput(Span<bool> values)
        {
        }

        public void WriteOutput(int startIndex, Span<bool> value)
        {
        }

        public void AllocateRegisters(int count)
        {
        }

        public BitsValue ReadRegister(int index)
        {
            return 0;
        }

        public void WriteRegister(int index, BitsValue value)
        {
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