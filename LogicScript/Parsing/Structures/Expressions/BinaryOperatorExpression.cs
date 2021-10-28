namespace LogicScript.Parsing.Structures.Expressions
{
    internal class BinaryOperatorExpression : Expression
    {
        public Operator Operator { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public override bool IsConstant => Left.IsConstant && Right.IsConstant;
        public override int BitSize => Operator switch
        {
            Operator.And or Operator.Or or Operator.Xor => Left.BitSize > Right.BitSize ? Left.BitSize : Right.BitSize,
            Operator.EqualsCompare or Operator.Greater or Operator.Lesser => 1,
            _ => throw new ParseException("Unknown operator bitsize", Location)
        };

        public BinaryOperatorExpression(SourceLocation location, Operator op, Expression left, Expression right) : base(location)
        {
            this.Operator = op;
            this.Left = left;
            this.Right = right;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
