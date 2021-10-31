using System;

namespace LogicScript.Parsing
{
    public class ParseException : Exception
    {
        public SourceSpan Span { get; }

        public ParseException(string message, SourceSpan span) : base(message)
        {
            this.Span = span;
        }

        public ParseException(string message, SourceSpan span, Exception innerException) : base(message, innerException)
        {
            this.Span = span;
        }
    }
}
