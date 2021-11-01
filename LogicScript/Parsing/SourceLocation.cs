using Antlr4.Runtime;
using System;

namespace LogicScript.Parsing
{
    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        public int Line { get; }
        public int Column { get; }

        internal SourceLocation(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }
        internal SourceLocation(IToken token) : this(token.Line, token.Column + 1)
        {
        }

        public override string ToString() => $"{Line}:{Column}";

        public override bool Equals(object obj) => obj is SourceLocation other && Equals(other);

        public bool Equals(SourceLocation other)
            => other.Line == Line && other.Column == Column;
    }
}
