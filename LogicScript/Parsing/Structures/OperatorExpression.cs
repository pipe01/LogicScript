namespace LogicScript.Parsing.Structures
{
    internal class OperatorExpression : Expression
    {
        public override bool IsSingleBit { get; }
        public override ExpressionType Type => ExpressionType.Operator;
        public override bool IsReadable => true;

        public Operator Operator { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public OperatorExpression(Operator op, Expression left, Expression right, SourceLocation location) : base(location)
        {
            this.Operator = op;
            this.Left = left;
            this.Right = right;

            this.IsSingleBit = (op >= Operator.Equals && op <= Operator.LesserOrEqual) || (left.IsSingleBit && right.IsSingleBit);
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
