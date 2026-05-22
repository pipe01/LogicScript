using LogicScript.Data;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class NumberLiteralExpression(SourceSpan span, BitsValue value) : Expression(span)
    {
        public BitsValue Value { get; } = value;

        public override bool IsConstant => true;
        public override int BitSize => Value.Length;

        public override string ToString() => Value.ToString();
    }
}
