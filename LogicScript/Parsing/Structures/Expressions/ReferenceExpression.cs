namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression : Expression
    {
        public Reference Reference { get; set; }

        public override bool IsConstant => false;

        public ReferenceExpression(SourceLocation location, Reference target) : base(location)
        {
            this.Reference = target;
        }

        public override string ToString() => Reference.ToString();
    }
}
