using System;
using System.Numerics;

namespace LogicScript.Data
{
    public readonly struct BitsValue
    {
        public const int BitSize = 64;
        public static readonly BitsValue Zero = new BitsValue(0, 1);
        public static readonly BitsValue One = new BitsValue(1, 1);

        public readonly ulong Number;
        public readonly int Length;

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
        internal int PopulationCount
        {
            get
            {
#if NETCOREAPP3_1
                return BitOperations.PopCount(Number);
#else
                int count = 0;
                var n = Number;
                while (n > 0)
                {
                    n &= (n - 1);
                    count++;
                }
                return count;
#endif
            }
        }

        private ulong OneMask => (1UL << Length) - 1;

        public bool[] Bits
        {
            get
            {
                var bits = new bool[Length];
                FillBits(bits);
                return bits;
            }
        }

        public bool this[int bitIndex] => ((Number >> (Length - 1 - bitIndex)) & 1) == 1;

        public BitsValue(ulong number, int length)
        {
            this.Number = number;
            this.Length = length;
        }

        public BitsValue(ulong number) : this(number, BitSize)
        {
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

        public override string ToString() => Number.ToString();

        public override bool Equals(object obj)
        {
            if (!(obj is BitsValue other))
                return false;

            return other == this;
        }

        public void FillBits(Span<bool> span)
        {
            if (span.Length != this.Length)
                throw new ArgumentException("Length mismatch when trying to fill span with bits");

            for (int i = 0; i < Length; i++)
            {
                span[i] = this[i];
            }
        }

        public void FillBits(Span<bool> span, int start, int end)
        {
            for (int i = 0; i < end - start; i++)
            {
                span[i] = this[i + start];
            }
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

        internal BitsValue Truncate(int length)
        {
            ulong mask = (1UL << length) - 1;

            return new BitsValue(Number & mask, length);
        }

        public static BitsValue FromBool(bool b) => b ? One : Zero;

        public static implicit operator BitsValue(ulong n) => new BitsValue(n);
        public static implicit operator BitsValue(long n) => new BitsValue((ulong)n);
        public static implicit operator BitsValue(uint n) => new BitsValue(n);
        public static implicit operator BitsValue(int n) => new BitsValue((ulong)n);
        public static implicit operator BitsValue(bool n) => n ? One : Zero;

        public static implicit operator ulong(BitsValue v) => v.Number;

        public static bool operator ==(BitsValue left, BitsValue right) => left.Number == right.Number;
        public static bool operator !=(BitsValue left, BitsValue right) => left.Number != right.Number;
    }
}
