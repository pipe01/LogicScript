using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using LogicScript;
using LogicScript.Data;
using System;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
#if RELEASE || true
            if (args.Length > 0 && args[0] == "--stress")
                Benchmarks.StressTest();
            else
                BenchmarkRunner.Run<Benchmarks>(ManualConfig.Create(DefaultConfig.Instance).With(MemoryDiagnoser.Default));

            return;
#else

            const string script = @"
@precompute off

any
    nice = 1010 * 2' ^ 5'

    out = nice[0..3']
end
";

            var result = Script.Compile(script);

            foreach (var item in result.Errors)
            {
                Console.WriteLine(item);
            }

            if (result.Errors.ContainsErrors)
            {
                Console.ReadKey(true);
                return;
            }

            var machine = new Machine();

            for (int i = 0; i < 1; i++)
            {
                LogicRunner.RunScript(result.Script, machine, i == 0);
                Console.WriteLine();
            }

            Console.ReadKey(true);
#endif
        }
    }

    public class Machine : IMachine
    {
        public int InputCount => Inputs.Length;
        public int OutputCount => 99;

        public IMemory Memory { get; } = new VolatileMemory();

        private readonly bool[] Inputs = new[] { true, false, true, false };

        public void SetOutputs(int start, Span<bool> values)
        {
            var bitsVal = new BitsValue(values);
            Console.WriteLine($"Set outputs [{start}..{start + values.Length}] to {bitsVal.Number} ({bitsVal})");
        }

        public void GetInputs(int start, Span<bool> inputs)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = Inputs[i + start];
            }

            Console.WriteLine($"Read inputs [{start}..{start + inputs.Length}]");
        }
    }
}
