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

        public override string ToString() => $"{Line + 1}:{Column + 1}";
    }
}
