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
        public bool IsOne => Number == 1;
        public bool AreAllBitsSet => Number != 0 && (Number == OneMask);
        public bool IsAnyBitSet => Number != 0;
        internal BitsValue Negated => new BitsValue(OneMask ^ Number, Length);
        internal BitsValue Truncated
        {
            get
            {
                ulong n = Number;
                int len = 0;

                while (n > 0)
                {
                    n >>= 1;
                    len++;
                }

                return new BitsValue(Number, len);
            }
        }

        private ulong OneMask => (1UL << Length) - 1;

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

        public bool this[int bitIndex] => ((Number >> (Length - 1 - bitIndex)) & 1) == 1;

        public BitsValue(ulong number, int? length = null)
        {
            this.Number = number;
            this.Length = length ?? BitSize;
        }

        public BitsValue(Span<bool> bits)
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

        public override string ToString() => string.Join("", Bits.Take(Length).Select(o => o ? 1 : 0));

        public override bool Equals(object obj)
        {
            if (!(obj is BitsValue other))
                return false;

            return other == this;
        }

        public override int GetHashCode() => Number.GetHashCode();

        internal bool AggregateBits(bool start, Func<bool, bool, bool> aggregator, bool? shortCircuitOn = null)
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

        public static implicit operator ulong(BitsValue v) => v.Number;

        public static bool operator ==(BitsValue left, BitsValue right) => left.Number == right.Number;
        public static bool operator !=(BitsValue left, BitsValue right) => left.Number != right.Number;
    }
}
