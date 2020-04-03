namespace LogicScript.Parsing.Structures
{
    internal class OperatorExpression : Expression
    {
        public override bool IsSingleBit { get; }

        public Operator Operator { get; }
        public Expression Left { get; }
        public Expression Right { get; }

        public OperatorExpression(Operator @operator, Expression left, Expression right, SourceLocation location) : base(location)
        {
            this.Operator = @operator;
            this.Left = left;
            this.Right = right;

            this.IsSingleBit = left.IsSingleBit && right.IsSingleBit;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
