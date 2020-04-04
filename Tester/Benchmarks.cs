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
when (in[1], in[2]) = 11
    out = 13'
    out = add(10', 4')
    out = add(10, 01)
    out = and(10, 01)
    out = or(10, 01)

    out[0] = 1
    out[0] = and(1, 1)
    out[0] = or(1, 1)
end
";

        private Script Compiled;

        [GlobalSetup]
        public void Setup()
        {
            Compiled = Script.Compile(RawScript).Script;
        }

        [Benchmark]
        public void CompileScript()
        {
            Script.Compile(RawScript);
        }

        [Benchmark]
        public void RunScript()
        {
            LogicRunner.RunScript(Compiled, NoopMachine.Instance);
        }

        private class NoopMachine : IMachine
        {
            public static NoopMachine Instance { get; } = new NoopMachine();

            public int InputCount { get; } = 5;
            public int OutputCount { get; } = 5;

            public bool GetInput(int i) => true;

            public BitsValue GetInputs() => 0;

            public void SetOutput(int i, bool on)
            {
            }

            public void SetOutputs(BitsValue values)
            {
            }
        }
    }
}
