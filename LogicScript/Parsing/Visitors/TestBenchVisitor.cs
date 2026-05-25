using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using LogicScript.Testing;

namespace LogicScript.Parsing.Visitors
{
    internal class TestBenchVisitor(ErrorSink errors) : LogicScriptBaseVisitor<TestBench>
    {
        public override TestBench VisitTest_bench([NotNull] LogicScriptParser.Test_benchContext context)
        {
            var cases = new List<TestCase>();

            int i = 0;
            foreach (var testCase in context.test_case())
            {
                cases.Add(new TestCaseVisitor(null, i++, errors).VisitTest_case(testCase));
            }

            return new TestBench(cases);
        }
    }
}