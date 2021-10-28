using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;

namespace LogicScript.Parsing
{
    internal class ErrorListener : IAntlrErrorListener<IToken>
    {
        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            throw new ParseException(msg, new SourceLocation(line, charPositionInLine), e);
        }
    }
}
