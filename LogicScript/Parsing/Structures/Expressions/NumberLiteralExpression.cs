using LogicScript.Data;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class NumberLiteralExpression : Expression
    {
        public BitsValue Value { get; }

        public NumberLiteralExpression(SourceLocation location, BitsValue value) : base(location)
        {
            this.Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
