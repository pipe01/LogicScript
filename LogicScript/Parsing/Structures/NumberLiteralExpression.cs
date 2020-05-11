using LogicScript.Data;

namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public override bool IsSingleBit => Length == 1;
        public override bool IsReadable => true;

        public ulong Value { get; set; }
        public int Length { get; set; }

        public NumberLiteralExpression(SourceLocation location, ulong value, int? length = null) : base(location)
        {
            this.Value = value;
            this.Length = length ?? BitsValue.BitSize;
        }

        public override string ToString() => Value.ToString();
    }
}
