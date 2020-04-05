namespace LogicScript.Parsing.Structures
{
    internal class SlotExpression : Expression
    {
        public override bool IsSingleBit => IsIndexed;

        public Slots Slot { get; }
        public int? Index { get; }

        public bool IsIndexed => Index != null;

        public SlotExpression(Slots slot, int? index, SourceLocation location) : base(location)
        {
            this.Slot = slot;
            this.Index = index;
        }

        public override string ToString() => $"{Slot}{(Index != null ? $"[{Index}]" : "")}";
    }
}
