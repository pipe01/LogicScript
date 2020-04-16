namespace LogicScript.Data
{
    public readonly struct BitRange
    {
        /// <summary>
        /// Inclusive start index.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Exclusive end index.
        /// </summary>
        public int End { get; }

        public int Length => End - Start;
        public bool HasEnd => End >= 0;

        public BitRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public override string ToString() => HasEnd && End > Start + 1 ? $"{Start},{End}" : Start.ToString();
    }
}
