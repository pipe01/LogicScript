using System;
using System.Linq;

namespace LogicScript.Data
{
    public readonly struct BitsValue
    {
        private const int BitSize = 64;
        public static readonly BitsValue Zero = new BitsValue();

        private readonly long Number;

        public bool IsSingleBit { get; }

        public ReadOnlyMemory<bool> Bits
        {
            get
            {
                var bits = new bool[BitSize];
                for (int i = 0; i < BitSize; i++)
                {
                    bits[i] = this[i];
                }
                return bits;
            }
        }

        public bool this[int bitIndex] => ((Number >> (BitSize - 1 - bitIndex)) & 1) == 1;

        internal BitsValue(long number)
        {
            this.Number = number;
            this.IsSingleBit = number == 0 || number == 1;
        }

        internal BitsValue(bool[] bits)
        {
            if (bits.Length > BitSize)
                throw new ArgumentException($"Maximum bit size is {BitSize}");

            long n = 0;

            for (int i = bits.Length - 1; i >= 0; i--)
            {
                if (bits[i])
                    n |= 1L << (bits.Length - 1 - i);
            }

            this.Number = n;
            this.IsSingleBit = bits.Length == 1;
        }

        public override string ToString() => string.Join("", Bits.ToArray().SkipWhile(o => !o).Select(o => o ? 1 : 0));

        public static implicit operator BitsValue(long n) => new BitsValue(n);

        public static bool operator ==(BitsValue left, BitsValue right) => left.Number == right.Number;
        public static bool operator !=(BitsValue left, BitsValue right) => left.Number != right.Number;
        public static bool operator >(BitsValue left, BitsValue right) => left.Number > right.Number;
        public static bool operator <(BitsValue left, BitsValue right) => left.Number < right.Number;

        public static BitsValue operator &(BitsValue left, BitsValue right) => left.Number & right.Number;
        public static BitsValue operator |(BitsValue left, BitsValue right) => left.Number | right.Number;
    }
}
