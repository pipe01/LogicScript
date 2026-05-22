using Antlr4.Runtime;
using System.IO;

namespace LogicScript.Parsing
{
    internal class ErrorListener(ErrorSink errors) : IAntlrErrorListener<IToken>
    {
        private readonly ErrorSink Errors = errors;

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.AddError(msg, new SourceSpan(offendingSymbol), isANTLR: true);
        }
    }
}
