using Antlr4.Runtime;
using System;

namespace LogicScript.Parsing
{
    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        public string FileName { get; }
        public int Line { get; }
        public int Column { get; }

        internal SourceLocation(string fileName, int line, int column)
        {
            this.FileName = fileName;
            this.Line = line;
            this.Column = column;
        }
        internal SourceLocation(IToken token) : this(token.TokenSource.SourceName, token.Line, token.Column + 1)
        {
        }

        public override string ToString() => $"{Line}:{Column}";

        public override bool Equals(object? obj) => obj is SourceLocation other && Equals(other);

        public bool Equals(SourceLocation other)
            => other.Line == Line && other.Column == Column;

        public override int GetHashCode() => HashCode.Combine(FileName, Line, Column);
    }
}
