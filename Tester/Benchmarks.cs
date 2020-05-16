using BenchmarkDotNet.Attributes;
using LogicScript;
using System;
using System.Linq;

namespace Tester
{
    public class Benchmarks
    {
        public const string RawScript = @"@precompute off
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

        private readonly Action<IMachine> Compiled;
        private readonly Script Script;
        private readonly IMachine Machine = new Machine { Noop = true };

        public Benchmarks()
        {
            var result = Script.Compile(RawScript);

            if (result.Errors.ContainsErrors)
                throw new InvalidOperationException("Script failed to compile");

            Compiled = new Compiler().Compile(result.Script).First();
            Script = result.Script;
        }

        [Benchmark]
        public void RunCompiled()
        {
            Compiled(Machine);
        }

        [Benchmark]
        public void RunInterpreted()
        {
            LogicRunner.RunScript(Script, Machine);
        }
    }
}
