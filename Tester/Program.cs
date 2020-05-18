using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using LogicScript;
using LogicScript.Data;
using System;
using System.Linq;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
#if RELEASE
            BenchmarkRunner.Run<Benchmarks>(ManualConfig.Create(DefaultConfig.Instance).With(MemoryDiagnoser.Default));
#else
            var result = Script.Compile(@"
any
    out = in[1..3]
end
");

            int a = 10;
            Span<bool> vals = stackalloc bool[a];

            foreach (var item in result.Errors)
            {
                Console.WriteLine(item);
            }

            if (result.Errors.ContainsErrors)
            {
                Console.ReadKey(true);
                return;
            }

            var firstCase = new Compiler().Compile(result.Script).First();
            firstCase(new Machine());
            Console.ReadKey(true);
#endif
        }
    }

    public class Machine : IMachine
    {
        public int InputCount => Inputs.Length;
        public int OutputCount => 99;

        public bool Noop { get; set; }

        public IMemory Memory { get; } = new VolatileMemory();

        private readonly bool[] Inputs = new[] { true, false, true, false };

        public void SetOutputs(int start, BitsValue values)
        {
            if (Noop)
                return;

            var bitsVal = new BitsValue(values);
            Console.WriteLine($"Set outputs [{start}..{start + values.Length}] to {bitsVal.Number} ({bitsVal})");
        }

        public void GetInputs(int start, Span<bool> values)
        {
            if (Noop)
                return;

            Console.WriteLine($"Read inputs [{start}..]");

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Inputs[i + start];
            }
        }
    }
}
