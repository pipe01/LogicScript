using Antlr4.Runtime;
using System;
using System.Text;

namespace LogicScript.Parsing
{
    public readonly struct SourceSpan : IEquatable<SourceSpan>, IComparable<SourceSpan>
    {
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        public SourceSpan(SourceLocation start, SourceLocation end)
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

        public bool Contains(SourceLocation loc, bool sameFile = true)
        {
            if (sameFile && loc.FileName != Start.FileName)
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

        public string GetText(string source)
        {
            if (Start.FileName != End.FileName)
                throw new InvalidOperationException("Cannot get text for a span that spans multiple files");

            var lines = source.Split('\n');
            if (Start.Line < 1 || Start.Line > lines.Length || End.Line < 1 || End.Line > lines.Length)
                throw new ArgumentOutOfRangeException("Span is out of range of the source text");

            if (Start.Line == End.Line)
            {
                var line = lines[Start.Line - 1];
                return line.Substring(Start.Column - 1, End.Column - Start.Column);
            }
            else
            {
                var sb = new StringBuilder();

                sb.AppendLine(lines[Start.Line - 1][(Start.Column - 1)..]);

                for (int i = Start.Line; i < End.Line - 1; i++)
                    sb.AppendLine(lines[i]);

                sb.Append(lines[End.Line - 1][..(End.Column - 1)]);

                return sb.ToString();
            }
        }

        public int CompareTo(SourceSpan other)
        {
            int fileComparison = string.Compare(Start.FileName, other.Start.FileName, StringComparison.Ordinal);
            if (fileComparison != 0)
                return fileComparison;

            int startLineComparison = Start.Line.CompareTo(other.Start.Line);
            if (startLineComparison != 0)
                return startLineComparison;

            int startColumnComparison = Start.Column.CompareTo(other.Start.Column);
            if (startColumnComparison != 0)
                return startColumnComparison;

            int endLineComparison = End.Line.CompareTo(other.End.Line);
            if (endLineComparison != 0)
                return endLineComparison;

            return End.Column.CompareTo(other.End.Column);
        }
    }
}
