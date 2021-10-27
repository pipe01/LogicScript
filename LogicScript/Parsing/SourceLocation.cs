using Antlr4.Runtime;

namespace LogicScript.Parsing
{
    public readonly struct SourceLocation
    {
        public int Line { get; }
        public int Column { get; }

        internal SourceLocation(int line, int column)
        {
            this.Line = line;
            this.Column = column;
        }
        internal SourceLocation(IToken token) : this(token.Line, token.Column)
        {
        }

        public override string ToString() => $"{Line + 1}:{Column + 1}";
    }
}
