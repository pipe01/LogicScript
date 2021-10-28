using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript;
using LogicScript.Parsing;
using LogicScript.Parsing.Visitors;
using System;

namespace Tester
{
    static class Program
    {
        class Listener : IAntlrErrorListener<IToken>
        {
            public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
            {
                throw new ParseException(msg, new SourceLocation(line, charPositionInLine), e);
            }
        }

        static void Main(string[] args)
        {
            var input = new AntlrInputStream(@"input asd
input test
output'2 out
asd
reg'123 on

when *
    on = out
end

");
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new Listener());

            var script = new ScriptVisitor().Visit(parser.script());
        }
    }
}
