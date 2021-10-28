namespace LogicScript.Parsing.Structures.Expressions
{
    internal class BinaryOperatorExpression : Expression
    {
        public Operator Operator { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public BinaryOperatorExpression(SourceLocation location, Operator op, Expression left, Expression right) : base(location)
        {
            this.Operator = op;
            this.Left = left;
            this.Right = right;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
