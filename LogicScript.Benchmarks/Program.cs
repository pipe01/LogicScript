#nullable disable

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LogicScript.Data;
using LogicScript.Compiling;
using System.Runtime.CompilerServices;
using LogicScript.Parsing;

namespace LogicScript.Benchmarks
{
    [MemoryDiagnoser]
    public class LogicScriptBenchmark
    {
        private record struct TestCase(int Inputs, int Outputs, string Source);

        private static readonly TestCase[] TestCases = [
            new(0, 0, @"
reg a
reg b
reg c

when 1
    c = a & b
end
"),
//             new(8 * 3, 8, @"input'8 a
// input'8 b
// input'8 c
// output'8 out

// when 1
//     out = b & c
//     out = b | c
//     out = b ^ c
//     out '= b << 3
//     out = b >> 3

//     out '= b + c
//     out '= b * c
//     out '= b / (c + 1)
//     // out '= b ** c
//     out '= b % (c + 1)

//     out = b == c
//     out = b != c
//     out = b > c
//     out = b < c
//     out = b < c

//     out = !b
//     out = len(b)
//     out = allOnes(b)
// end"),
        ];

        [Params(0)]
        public int TestIndex { get; set; }

        private TestCase Case;
        private IMachine Machine;
        private IScriptInstance ICompiledScript, CompiledScriptDebug, CompiledScriptWithDebugger;

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

            this.Machine = new DummyMachine(Case.Inputs, Case.Outputs);

            this.ICompiledScript = Compiler.Compile(script).Instantiate(Machine);
            this.CompiledScriptDebug = Compiler.Compile(script, true).Instantiate(Machine);
            this.CompiledScriptWithDebugger = Compiler.Compile(script, true).Instantiate(Machine);
            this.CompiledScriptWithDebugger.Debugger = DummyDebugger.Instance;
        }

        interface IRunner
        {
            void Run();
        }
        class Runner : IRunner
        {
            public byte A, B, C;
            void IRunner.Run()
            {
                C = (byte)(A & B);
            }
        }

        private readonly IRunner _Runner = new Runner();
        [Benchmark(Baseline = true)]
        public void RunCSharp()
        {
            _Runner.Run();
        }

        [Benchmark]
        public void RunCompiledNoDebug()
        {
            ICompiledScript.Run();
        }

        [Benchmark]
        public void RunCompiledDebug()
        {
            CompiledScriptDebug.Run();
        }

        [Benchmark]
        public void RunCompiledDebugWithDebugger()
        {
            CompiledScriptWithDebugger.Run();
        }
    }

    class DummyMachine(int inputCount, int outputCount) : IMachine
    {
        public int InputCount { get; } = inputCount;
        public int OutputCount { get; } = outputCount;

        private readonly bool[] Inputs = new bool[inputCount];
        private readonly bool[] Outputs = new bool[outputCount];

        public void Print(string msg)
        {
        }

        public BitsValue ReadInputs(int startIndex, int count)
        {
            return new(Inputs.AsSpan()[startIndex..(startIndex + count)]);
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

        public void QueueUpdate()
        {
        }
    }

    class DummyDebugger : IDebugger
    {
        public static readonly DummyDebugger Instance = new();

        public void GotOutput(string line)
        {
        }

        public void PushFunctionCall(NodeID id)
        {
        }


        public void PopFunctionCall()
        {
        }

        public void PopLocal(NodeID id)
        {
        }
        public void PushLocal(NodeID id)
        {
        }

        public void SetLocal(NodeID id, ulong value)
        {
        }

        public void TraceStatement(IScriptInstance compiledScript, IMachine machine, NodeID id)
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