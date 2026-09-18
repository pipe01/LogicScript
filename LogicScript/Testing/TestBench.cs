using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using LogicScript.Compiling;
using LogicScript.Parsing;
using LogicScript.Parsing.Visitors;
using LogicScript.Testing.Results;

namespace LogicScript.Testing
{
    public class TestBench
    {
        private readonly IList<TestCase> Cases;

        public int CaseCount => Cases.Count;
        public IReadOnlyCollection<string?> CaseNames => Cases.Select(o => o.Name).ToArray();

        internal TestBench(IList<TestCase> cases)
        {
            this.Cases = cases;
        }

        public async IAsyncEnumerable<CaseResult> Run(ICompiledScript runner, Script script, IDebugger? debugger)
        {
            var machine = new TestingMachine(script.RegisteredInputLength, script.RegisteredOutputLength);

            foreach (var @case in Cases)
            {
                machine.Reset();
                runner.HasRun = false;
                runner.Registers.Reset();

                yield return await @case.Run(runner, script, machine, debugger);
            }
        }

        public static (TestBench? Bench, IReadOnlyList<Error> Errors) Parse(string source)
        {
            var errors = new ErrorSink();

            var input = new AntlrInputStream(source.Replace("\r\n", "\n") + "\n");
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener(errors));

            var visitor = new TestBenchVisitor(errors);

            return (visitor.VisitTest_bench(parser.test_bench()), errors);
        }
    }
}