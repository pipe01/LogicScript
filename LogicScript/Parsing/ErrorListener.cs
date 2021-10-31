using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;

namespace LogicScript.Parsing
{
    internal class ErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly ErrorSink Errors;

        public ErrorListener(ErrorSink errors)
        {
            this.Errors = errors;
        }

        public void SyntaxError([NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.AddError(msg, new SourceSpan(offendingSymbol), isANTLR: true);
        }
    }
}
