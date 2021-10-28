namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class TruncateExpression : Expression
    {
        public Expression Operand { get; set; }
        public int Size { get; set; }

        public override bool IsConstant => Operand.IsConstant;
        public override int BitSize => Size;

        public TruncateExpression(SourceLocation location, Expression operand, int size) : base(location)
        {
            this.Operand = operand;
            this.Size = size;
        }
    }
}
