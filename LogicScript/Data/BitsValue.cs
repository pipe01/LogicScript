using System;
using System.Linq;

namespace LogicScript.Data
{
    public readonly struct BitsValue
    {
        private const int BitSize = 64;
        public static readonly BitsValue Zero = new BitsValue(0, 1);
        public static readonly BitsValue One = new BitsValue(1, 1);

        public ulong Number { get; }

        public int Length { get; }

        public bool IsSingleBit => Number == 0 || Number == 1;

        public bool AreAllBitsSet => Number != 0 && (((Number + 1) & Number) == 0);

        public bool[] Bits
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

        internal BitsValue(ulong number, int? length = null)
        {
            this.Number = number;
            this.Length = length ?? BitSize;
        }

        internal BitsValue(bool[] bits)
        {
            if (bits.Length > BitSize)
                throw new ArgumentException($"Maximum bit size is {BitSize}");

            ulong n = 0;

            for (int i = bits.Length - 1; i >= 0; i--)
            {
                if (bits[i])
                    n |= 1UL << (bits.Length - 1 - i);
            }

            this.Number = n;
            this.Length = bits.Length;
        }

        public override string ToString() => string.Join("", Bits.ToArray().SkipWhile(o => !o).Select(o => o ? 1 : 0));

        public override bool Equals(object obj)
        {
            if (!(obj is BitsValue other))
                return false;

            return other == this;
        }

        public override int GetHashCode() => Number.GetHashCode();

        public bool AggregateBits(bool start, Func<bool, bool, bool> aggregator, bool? shortCircuitOn = null)
        {
            bool value = start;

            for (int i = 0; i < Length; i++)
            {
                value = aggregator(value, this[i]);

                if (value == shortCircuitOn)
                    break;
            }

            return value;
        }

        public static implicit operator BitsValue(ulong n) => new BitsValue(n);
        public static implicit operator BitsValue(bool n) => n ? One : Zero;

        public static implicit operator bool(BitsValue v) => v.IsSingleBit && v == One;

        public static bool operator ==(BitsValue left, BitsValue right) => left.Number == right.Number;
        public static bool operator !=(BitsValue left, BitsValue right) => left.Number != right.Number;
    }
}
