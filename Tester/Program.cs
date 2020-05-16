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
    out = 1 + 2
end
");

            var a = 2;
            var b = 4;
            var c = a > b ? a : b;

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

        public BitsValue GetInputs(int start)
        {
            if (Noop)
                return BitsValue.Zero;

            Console.WriteLine($"Read inputs [{start}..]");

            return new BitsValue(Inputs[start..]);
        }
    }
}
