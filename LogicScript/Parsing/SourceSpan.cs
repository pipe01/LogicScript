using Antlr4.Runtime;
using System;

namespace LogicScript.Parsing
{
    public readonly struct SourceSpan : IEquatable<SourceSpan>
    {
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        internal SourceSpan(SourceLocation start, SourceLocation end)
        {
            if (start.FileName != end.FileName)
                throw new ArgumentException("Start and end locations must be in the same file");

            if (start.Line > end.Line || (start.Line == end.Line && start.Column > end.Column))
                (start, end) = (end, start);

            this.Start = start;
            this.End = end;
        }

        internal SourceSpan(ParserRuleContext context) : this(context.Start, context.Stop)
        {
        }

        internal SourceSpan(IToken start, IToken end) : this(new SourceLocation(start), new SourceLocation(end.TokenSource.SourceName, end.Line, end.Column + end.Text.Length + 1))
        {
        }

        internal SourceSpan(IToken token) : this(token, token)
        {
        }

        internal SourceSpan(string fileName, int lineStart, int colStart, int lineEnd, int colEnd) : this(new SourceLocation(fileName, lineStart, colStart), new SourceLocation(fileName, lineEnd, colEnd))
        {
        }

        public bool Contains(SourceLocation loc)
        {
            if (loc.FileName != Start.FileName)
                return false;

            if (loc.Line < Start.Line || loc.Line > End.Line)
                return false;

            if (loc.Line == Start.Line && loc.Column < Start.Column)
                return false;

            if (loc.Line == End.Line && loc.Column > End.Column)
                return false;

            return true;
        }

        public override string ToString() => $"{Start.FileName}:{Start} to {End}";

        public override bool Equals(object? obj) => obj is SourceSpan other && Equals(other);

        public bool Equals(SourceSpan other) => other.Start.Equals(Start) && other.End.Equals(End);

        public override int GetHashCode() => HashCode.Combine(Start, End);
    }
}
