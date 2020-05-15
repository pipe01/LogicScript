using BenchmarkDotNet.Attributes;
using LogicScript;
using LogicScript.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    public class Benchmarks
    {
        private const string RawScript =
            @"
when (in[1], in[2]) == 11
    out = 13'
    out = 10' + 4'
    out = 10 + 01
    out = 10 & 01
    out = 10 | 01

    out[0] = 1
    out[0..] = 1 & 1
    out[0] = 1 | 1
end
";

        private Script Compiled;

        [GlobalSetup]
        public void Setup()
        {
            Compiled = Script.Compile(RawScript).Script;
        }

        //[Benchmark]
        //public void CompileScript()
        //{
        //    Script.Compile(RawScript);
        //}

        [Benchmark]
        public void RunScript()
        {
            LogicRunner.RunScript(Compiled, NoopMachine.Instance);
        }

        public static void StressTest()
        {
            const int iterations = 10000000;

            var result = Script.Compile(@"
any
    mem[0..3] = 101
    out[0..2] = mem[0..2]
    out[0..] = 10101
    out[0..] = in[0..2]
end

when (in[1], in[2]) == 11
    out = 13'
    out = 10' + 4'
    out = 10 + 01
    out = 10 & 01
    out = 10 | 01

    out[0] = 1
    out[0..] = 1 & 1
    out[0] = 1 | 1
end
");

            for (int i = 0; i < iterations; i++)
            {
                LogicRunner.RunScript(result.Script, NoopMachine.Instance);

                if (i % 1000000 == 0)
                    Console.WriteLine((((double)i / iterations) * 100) + "%");
            }
        }

        private class NoopMachine : IMachine
        {
            public static NoopMachine Instance { get; } = new NoopMachine();

            public int InputCount { get; } = 5;
            public int OutputCount { get; } = 5;
            public IMemory Memory { get; } = new VolatileMemory();

            public void GetInputs(int start, Span<bool> inputs)
            {
            }

            public void SetOutputs(int start, Span<bool> values)
            {
            }

            public void SetOut(BitsValue value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
