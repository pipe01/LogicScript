namespace LogicScript.Parsing.Structures
{
    internal class InputExpression : Expression
    {
        public int InputIndex { get; }

        public override bool IsSingleBit => true;

        public InputExpression(int inputIndex, SourceLocation location) : base(location)
        {
            this.InputIndex = inputIndex;
        }

        public override string ToString() => $"in[{InputIndex}]";
    }
}
