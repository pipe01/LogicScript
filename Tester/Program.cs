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
#if RELEASE
            BenchmarkRunner.Run<Benchmarks>(ManualConfig.Create(DefaultConfig.Instance).With(MemoryDiagnoser.Default));
#else
            const string script = @"@precompute off
any
    out = 1 > 2'    # 0
    out = 3' > 2'   # 1

    out = 1 >= 2'   # 0
    out = 2' >= 2'  # 1
    out = 3' >= 2'  # 1

    out = 1 == 2    # 0
    out = 1 == 1    # 1

    out = 1 != 2    # 1
    out = 1 != 1    # 0

    out = 1 <= 2'   # 1
    out = 2' <= 2'  # 1
    out = 3' <= 2'  # 0

end
";

            var a = 5;
            var b = 7;
            var c = a <= b;


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

            new Compiler().Compile(result.Script, new Machine());
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

        public void SetOutputs(int start, BitsValue values)
        {
            var bitsVal = new BitsValue(values);
            Console.WriteLine($"Set outputs [{start}..{start + values.Length}] to {bitsVal.Number} ({bitsVal})");
        }

        public BitsValue GetInputs(int start)
        {
            Console.WriteLine($"Read inputs [{start}..]");

            return new BitsValue(Inputs[start..]);
        }
    }
}
