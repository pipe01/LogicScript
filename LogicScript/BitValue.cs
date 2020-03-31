using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicScript
{
    internal abstract class BitValue
    {

    }

    internal class LiteralBitValue : BitValue
    {
        public int Length { get; }

        public bool this[int index]
        {
            get
            {
                if (index > Length - 1 || index < 0)
                    throw new IndexOutOfRangeException();

                return ArrayValue == null
                    ? ((NumValue >> index) & 1) == 1
                    : ArrayValue[index];
            }
        }

        private readonly ulong NumValue;
        private readonly bool[]? ArrayValue;

        public LiteralBitValue(bool[] values, bool littleEndian = false)
        {
            if (values.Length < sizeof(long))
            {
                ulong n = 0;

                for (int i = 0; i < values.Length; i++)
                {
                    int index = littleEndian
                        ? i
                        : values.Length - 1 - i;

                    n |= (values[index] ? 1UL : 0UL) << i;
                }

                this.NumValue = n;
                this.ArrayValue = null;
            }
            else
            {
                this.NumValue = 0;
                this.ArrayValue = values;
            }

            this.Length = values.Length;
        }

        public LiteralBitValue(ulong num)
        {
            this.NumValue = num;
            this.ArrayValue = null;
            this.Length = sizeof(ulong);
        }

        public LiteralBitValue(uint num)
        {
            this.NumValue = num;
            this.ArrayValue = null;
            this.Length = sizeof(uint);
        }

        public static implicit operator LiteralBitValue(uint n) => new LiteralBitValue(n);
        public static implicit operator LiteralBitValue(ulong n) => new LiteralBitValue(n);
    }
}
