namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class PlaceholderExpression(SourceSpan span, int bitSize = 0) : Expression(span)
    {
        public override bool IsConstant => false;
        public override int BitSize => bitSize;
    }
}
