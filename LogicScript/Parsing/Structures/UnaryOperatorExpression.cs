using System.Linq;

namespace LogicScript.Parsing.Structures
{
    internal class UnaryOperatorExpression : Expression
    {
        public override bool IsSingleBit => Operand.IsSingleBit;
        public override ExpressionType Type => ExpressionType.UnaryOperator;
        public override bool IsReadable => true;

        public Operator Operator { get; set; }
        public Expression Operand { get; set; }

        public UnaryOperatorExpression(Operator @operator, Expression operand, SourceLocation location) : base(location)
        {
            this.Operand = operand;
            this.Operator = @operator;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
