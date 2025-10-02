using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.IO;

namespace LogicScript.Parsing
{
    internal class ErrorListener : IAntlrErrorListener<IToken>
    {
        private readonly ErrorSink Errors;

        public ErrorListener(ErrorSink errors)
        {
            this.Errors = errors;
        }

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.AddError(msg, new SourceSpan(offendingSymbol), isANTLR: true);
        }
    }
}
