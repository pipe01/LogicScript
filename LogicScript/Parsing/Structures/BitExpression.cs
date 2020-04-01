namespace LogicScript.Parsing.Structures
{
    internal abstract class BitExpression : Expression
    {
    }

    internal class LiteralBitExpression : BitExpression
    {
        public static readonly LiteralBitExpression True = new LiteralBitExpression(true);
        public static readonly LiteralBitExpression False = new LiteralBitExpression(false);

        public bool Value { get; }

        public LiteralBitExpression(bool value)
        {
            this.Value = value;
        }

        public override string ToString() => Value ? "1" : "0";
    }

    internal class InputBitExpression : BitExpression
    {
        public int InputIndex { get; }

        public InputBitExpression(int inputIndex)
        {
            this.InputIndex = inputIndex;
        }

        public override string ToString() => $"in[{InputIndex}]";
    }
}
