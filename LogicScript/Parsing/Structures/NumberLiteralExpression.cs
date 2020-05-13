using LogicScript.Data;

namespace LogicScript.Parsing.Structures
{
    internal class NumberLiteralExpression : Expression
    {
        public override bool IsSingleBit => Value.IsSingleBit;
        public override ExpressionType Type => ExpressionType.NumberLiteral;
        public override bool IsReadable => true;

        public BitsValue Value { get; set; }

        public NumberLiteralExpression(BitsValue value, SourceLocation location) : base(location)
        {
            this.Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
