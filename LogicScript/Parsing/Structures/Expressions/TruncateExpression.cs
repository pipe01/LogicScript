namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class TruncateExpression : Expression
    {
        public Expression Operand { get; set; }
        public int Size { get; set; }

        public override bool IsConstant => Operand.IsConstant;
        public override int BitSize => Size;

        public TruncateExpression(SourceSpan span, Expression operand, int size) : base(span)
        {
            this.Operand = operand;
            this.Size = size;
        }
    }
}
