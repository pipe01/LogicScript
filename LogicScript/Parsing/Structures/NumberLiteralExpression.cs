namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public long Value { get; }
        public int? Length { get; }

        public override bool IsSingleBit => Length == 1;

        public NumberLiteralExpression(SourceLocation location, long value, int? length = null) : base(location)
        {
            this.Value = value;
            this.Length = length;
        }

        public override string ToString() => Value.ToString();
    }
}
