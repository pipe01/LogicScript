using System;
using System.Linq;

namespace LogicScript.Data
{
    public readonly struct BitsValue
    {
        public static readonly BitsValue Zero = new BitsValue();

        private readonly bool[]? Values;
        private readonly int Number;

        public int Length { get; }

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
            this.Values = null;
            this.Length = Math.Max((int)Math.Log(number, 2) + 1, length ?? 0);
            this.Number = number;
        }

        public ReadOnlyMemory<bool> AsMemory()
        {
            if (Values != null)
                return Values;

            var values = new bool[Length];
            for (int i = 0; i < Length; i++)
            {
                values[i] = ((Number >> (Length - 1 - i)) & 1) == 1;
            }

            return values;
        }

        public override string ToString() => "(" + string.Join(", ", Values.Select(o => o ? 1 : 0)) + ")";
    }
}
