using Antlr4.Runtime;
using System;

namespace LogicScript.Parsing
{
    public readonly struct SourceSpan(string fileName, SourceLocation start, SourceLocation end) : IEquatable<SourceSpan>
    {
        public SourceLocation Start => start;
        public SourceLocation End => end;
        public string FileName => fileName;

        internal SourceSpan(ParserRuleContext context) : this(context.Start, context.Stop)
        {
        }

        internal SourceSpan(IToken start, IToken end) : this(start.TokenSource.SourceName, new SourceLocation(start), new SourceLocation(end.Line, end.Column + end.Text.Length + 1))
        {
        }

        internal SourceSpan(IToken token) : this(token, token)
        {
        }

        public bool Contains(SourceLocation loc)
            => loc.Line >= Start.Line && loc.Line <= End.Line && loc.Column >= Start.Column && loc.Column <= End.Column;

        public override string ToString() => $"{Start} to {End}";

        public override bool Equals(object? obj) => obj is SourceSpan other && Equals(other);

        public bool Equals(SourceSpan other) => other.Start.Equals(Start) && other.End.Equals(End);

        public override int GetHashCode() => HashCode.Combine(Start, End);
    }
}
