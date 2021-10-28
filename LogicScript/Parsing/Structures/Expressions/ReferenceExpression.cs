namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression : Expression
    {
        public Reference Target { get; set; }

        public override bool IsConstant => false;

        public ReferenceExpression(SourceLocation location, Reference target) : base(location)
        {
            this.Target = target;
        }

        public override string ToString() => Target.ToString();
    }
}
