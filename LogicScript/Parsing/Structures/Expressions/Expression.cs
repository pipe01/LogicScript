namespace LogicScript.Parsing.Structures.Expressions
{
    internal abstract class Expression : ICodeNode
    {
        public SourceLocation Location { get; }

        public abstract bool IsConstant { get; }
        public abstract int BitSize { get; }

        public Expression(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
