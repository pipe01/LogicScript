using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Visitors;
using System;
using System.Collections;
using System.Linq;

namespace Tester
{
    static class Program
    {
        class Listener : IAntlrErrorListener<IToken>
        {
            public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
            {
                throw new Exception($"{msg} on line {line}:{charPositionInLine}");
            }
        }

        static void Main(string[] args)
        {
            var input = new AntlrInputStream(@"input asd
input test
output[2] out

reg[123] on

when *
    1 & 2 | 3 == !(6 ^ 4) & 5
end

");
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new Listener());

            var script = new ScriptVisitor().Visit(parser.script());
            return;

#if RELEASE
            BenchmarkRunner.Run<Benchmarks>(ManualConfig.Create(DefaultConfig.Instance).With(MemoryDiagnoser.Default));
#else
            var result = Script.Compile(@"
any

end");

            foreach (var item in result.Errors)
            {
                Console.WriteLine(item);
            }

            if (result.Errors.ContainsErrors)
            {
                Console.ReadKey(true);
                return;
            }

            result.Script.Run(new Machine());

            Console.ReadKey(true);
#endif
        }
    }

    public class Machine : IMachine
    {
        public int InputCount => Inputs.Length;
        public int OutputCount => 99;

        public bool Noop { get; set; }

        public IMemory Memory { get; }

        private readonly bool[] Inputs = new[] { true, false, true, false };

        public void SetOutputs(int start, BitsValue values)
        {
            if (Noop)
                return;

            var bitsVal = new BitsValue(values);
            Console.WriteLine($"Set outputs [{start}..{start + values.Length}] to {bitsVal.Number} ({bitsVal})");
        }

        public BitsValue GetInputs(int start, int count)
        {
            if (Noop)
                return BitsValue.Zero;

            Console.WriteLine($"Read inputs [{start}..{start + count}]");

            return new BitsValue(Inputs[start..(start + count)]);
        }
    }
}
