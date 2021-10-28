namespace LogicScript.Parsing.Structures
{
    internal readonly struct LocalInfo
    {
        public int BitSize { get; }

        public LocalInfo(int bitSize)
        {
            this.BitSize = bitSize;
        }
    }
}
