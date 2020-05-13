namespace LogicScript.Parsing.Structures
{
    internal class SlotExpression : Expression
    {
        public override bool IsSingleBit => false;
        public override ExpressionType Type => ExpressionType.Slot;
        public override bool IsReadable => Slot == Slots.In || Slot == Slots.Memory;
        public override bool IsWriteable => Slot == Slots.Out || Slot == Slots.Memory;

        public Slots Slot { get; set; }

        public SlotExpression(Slots slot, SourceLocation location) : base(location)
        {
            this.Slot = slot;
        }

        public override string ToString() => Slot.ToString();
    }
}
