namespace LogicScript.Parsing.Structures
{
    internal class WholeInputExpression : Expression
    {
        public override bool IsSingleBit => false;

        public WholeInputExpression(SourceLocation location) : base(location)
        {
        }

        public override string ToString() => "in";
    }
}
