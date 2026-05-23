#nullable disable

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Compiling;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System.Runtime.CompilerServices;
using LogicScript.Interpreting.Debugging;

namespace LogicScript.Benchmarks
{
    [MemoryDiagnoser]
    public class LogicScriptBenchmark
    {
        private record struct TestCase(int Inputs, int Outputs, string Source);

        private static readonly TestCase[] TestCases = [
            new(2, 1, @"input a
input b
output c

when 1
    c = a & b
end
"),
            new(8 * 3, 8, @"input'8 a
input'8 b
input'8 c
output'8 out

when 1
    out = b & c
    out = b | c
    out = b ^ c
    out '= b << 3
    out = b >> 3

    out '= b + c
    out '= b * c
    out '= b / (c + 1)
    out '= b ** c
    out '= b % (c + 1)

    out = b == c
    out = b != c
    out = b > c
    out = b < c
    out = b < c

    out = !b
    out = len(b)
    out = allOnes(b)
end"),
        ];

        [Params(0, 1)]
        public int TestIndex { get; set; }

        private TestCase Case;
        private IMachine Machine;
        private Script Script;
        private CompiledScript CompiledScript, CompiledScriptDebug;
        private bool[] Scratch;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.Case = TestCases[TestIndex];

            var (script, errors) = Script.Parse(Case.Source);
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
            this.Script = script;

            this.Machine = new DummyMachine(Case.Inputs, Case.Outputs);

            this.Scratch = new bool[Math.Max(Machine.InputCount, Machine.OutputCount)];
            this.CompiledScript = Compiler.Compile(script);
            this.CompiledScriptDebug = Compiler.Compile(script);
        }

        [Benchmark]
        public void RunCompiledNoDebug()
        {
            CompiledScript(Machine, Scratch, false);
        }

        [Benchmark]
        public void RunInterpreted()
        {
            Interpreter.Run(Script, Machine, false);
        }

        [Benchmark(Baseline = true)]
        public void RunRaw()
        {
            Machine.WriteOutput(0, Machine.ReadInput(0) && Machine.ReadInput(1));
        }
    }

    class DummyMachine(int inputCount, int outputCount) : IMachine
    {
        public int InputCount { get; } = inputCount;
        public int OutputCount { get; } = outputCount;

        private readonly bool[] Inputs = new bool[inputCount];
        private readonly bool[] Outputs = new bool[outputCount];

        private ulong[] Registers = [];

        public void Print(string msg)
        {
        }

        public BitsValue ReadInputs()
        {
            return new(Inputs);
        }

        public bool ReadInput(int index)
        {
            return Inputs[index];
        }

        public void WriteOutputs(int startIndex, BitsValue value)
        {
            value.Bits.CopyTo(Outputs.AsSpan()[startIndex..]);
        }

        public void WriteOutput(int index, bool value)
        {
            Outputs[index] = value;
        }

        public void AllocateRegisters(int count)
        {
            Array.Resize(ref Registers, count);
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

    public class MyDebugger : IDebugger
    {
        public void TraceStatement(SourceSpan span, IDictionary<LocalInfo, ulong> locals)
        {
        }
    }

    public class DelegatesBenchmark
    {
        [Benchmark(Baseline = true)]
        public void CallMethod()
        {
            CalledMethod();
        }

        [Benchmark]
        public void CallAction()
        {
            Action();
        }

        [Benchmark]
        public void CallInterfaceMethod()
        {
            Class.Method();
        }

        [Benchmark]
        public unsafe void CallFunctionPointer()
        {
            FunctionPointer();
        }

        private static int I = 0;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CalledMethod()
        {
            I++;
        }

        private readonly Action Action = CalledMethod;
        private readonly unsafe delegate*<void> FunctionPointer;
        private readonly ICalledClass Class = new CalledClass();

        private interface ICalledClass
        {
            void Method();
        }
        private class CalledClass : ICalledClass
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Method()
            {
            }
        }

        unsafe public DelegatesBenchmark()
        {
            FunctionPointer = (delegate*<void>)Action.Method.MethodHandle.GetFunctionPointer();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}