namespace LogicScript.Parsing.Structures
{
    internal class SingleInputExpression : Expression
    {
        public int InputIndex { get; }

        public override bool IsSingleBit => true;

        public SingleInputExpression(int inputIndex, SourceLocation location) : base(location)
        {
            this.InputIndex = inputIndex;
        }

        public override string ToString() => $"in[{InputIndex}]";
    }
}
