using System.Linq;

namespace LogicScript.Parsing.Structures
{
    internal class UnaryOperatorExpression : Expression
    {
        public override bool IsSingleBit => Operand.IsSingleBit || Constants.AggregationOperators.Any(o => o.Value == Operator);
        public override bool IsReadable => true;

        public Operator Operator { get; }
        public Expression Operand { get; }

        public UnaryOperatorExpression(Operator @operator, Expression operand, SourceLocation location) : base(location)
        {
            this.Operand = operand;
            this.Operator = @operator;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
