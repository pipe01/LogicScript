using Antlr4.Runtime;

namespace LogicScript.Parsing
{
    public readonly struct SourceSpan
    {
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        internal SourceSpan(SourceLocation start, SourceLocation end)
        {
            this.Start = start;
            this.End = end;
        }

        internal SourceSpan(ParserRuleContext context) : this(context.Start, context.Stop)
        {
        }

        internal SourceSpan(IToken start, IToken end) : this(new SourceLocation(start), new SourceLocation(end))
        {
        }

        internal SourceSpan(IToken token) : this(token, token)
        {
        }
    }
}
