namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceLengthExpression(SourceSpan span, Reference reference) : Expression(span)
    {
        public Reference Reference { get; } = reference;

        public override bool IsConstant => true;
        public override int BitSize => 7;

        public override string ToString() => $"len({Reference})";
    }
}
