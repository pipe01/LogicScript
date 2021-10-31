namespace LogicScript.Parsing.Structures.Expressions
{
    internal abstract class Expression : ICodeNode
    {
        public SourceSpan Span { get; }

        public abstract bool IsConstant { get; }
        public abstract int BitSize { get; }

        public Expression(SourceSpan span)
        {
            this.Span = span;
        }
    }
}
