using LogicScript.Data;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class NumberLiteralExpression : Expression
    {
        public BitsValue Value { get; }

        public override bool IsConstant => true;
        public override int BitSize => Value.Length;

        public NumberLiteralExpression(SourceSpan span, BitsValue value) : base(span)
        {
            this.Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
