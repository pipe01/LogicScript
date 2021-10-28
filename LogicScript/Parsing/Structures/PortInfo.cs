namespace LogicScript.Parsing.Structures
{
    internal readonly struct PortInfo
    {
        public int StartIndex { get; }
        public int BitSize { get; }

        public PortInfo(int index, int bitSize)
        {
            this.StartIndex = index;
            this.BitSize = bitSize;
        }
    }
}
