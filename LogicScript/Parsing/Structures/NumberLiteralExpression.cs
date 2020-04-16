namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public override bool IsSingleBit => Length == 1;
        public override bool IsReadable => true;

        public ulong Value { get; }
        public int? Length { get; }

        public NumberLiteralExpression(SourceLocation location, ulong value, int? length = null) : base(location)
        {
            this.Value = value;
            this.Length = length;
        }

        public override string ToString() => Value.ToString();
    }
}
