using System;
using System.Linq;

namespace LogicScript.Data
{
    public readonly struct BitsValue
    {
        public static readonly BitsValue Zero = new BitsValue();

        private readonly bool[] Values;
        private readonly int Number;

        public int Length { get; }

        public ReadOnlyMemory<bool> Bits => Values;

        public bool this[int bitIndex]
        {
            get
            {
                if (Values == null)
                {
                    if (bitIndex < 0 || bitIndex > Length)
                        throw new IndexOutOfRangeException();

                    return ((Number >> (Length - 1 - bitIndex)) & 1) == 1;
                }
                else
                {
                    return Values[bitIndex];
                }
            }
        }

        internal BitsValue(bool[] values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
            this.Length = values.Length;
            this.Number = 0;
        }

        internal BitsValue(int number, int? length = null)
        {
            this.Length = Math.Max((int)Math.Log(number, 2) + 1, length ?? 0);
            this.Number = number;

            Values = new bool[Length];
            for (int i = 0; i < Length; i++)
            {
                Values[i] = ((Number >> (Length - 1 - i)) & 1) == 1;
            }
        }

        public override string ToString() => "(" + string.Join(", ", Values.Select(o => o ? 1 : 0)) + ")";

        public static BitsValue operator &(BitsValue left, BitsValue right)
        {
            int length = left.Length > right.Length ? left.Length : right.Length;
            bool[] values = new bool[length];

            for (int i = 0; i < length; i++)
            {
                bool lval = i >= left.Length ? false : left[i];
                bool rval = i >= right.Length ? false : right[i];

                values[i] = lval && rval;
            }

            return new BitsValue(values);
        }
    }
}
