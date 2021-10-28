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
output'2 out

reg'123 on

when *
    1 & 2 | 3 == !(6 ^ 4) & 5
end

");

        }
    }
}
