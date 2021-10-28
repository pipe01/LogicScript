namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class UnaryOperatorExpression : Expression
    {
        public Operator Operator { get; set; }
        public Expression Operand { get; set; }

        public override bool IsConstant => Operand.IsConstant;

        public UnaryOperatorExpression(SourceLocation location, Operator op, Expression operand) : base(location)
        {
            this.Operator = op;
            this.Operand = operand;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
