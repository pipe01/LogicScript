using LogicScript.Data;

namespace LogicScript.Parsing.Structures
{
    internal class SlotExpression : Expression
    {
        public override bool IsSingleBit => Range?.Length == 1;

        public Slots Slot { get; }
        public BitRange? Range { get; }

        public SlotExpression(Slots slot, BitRange? range, SourceLocation location) : base(location)
        {
            this.Slot = slot;
            this.Range = range;
        }

        public override string ToString() => $"{Slot}{(Range != null ? $"[{Range}]" : "")}";
    }
}
