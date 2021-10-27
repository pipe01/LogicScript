namespace LogicScript.Parsing.Structures
{
    internal readonly struct PortInfo
    {
        public int Index { get; }
        public int BitSize { get; }

        public PortInfo(int index, int bitSize)
        {
            this.Index = index;
            this.BitSize = bitSize;
        }
    }
}
