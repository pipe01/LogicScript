using System;
using System.Linq;

namespace LogicScript.Parsing.Structures
{
    internal readonly struct BitsValue
    {
        /// <summary>
        /// Big endian
        /// </summary>
        public BitValue[] Values { get; }

        public BitsValue(BitValue[] values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public BitsValue(uint number, int? length = null)
        {
            if (number == 0)
            {
                this.Values = length == null
                    ? new[] { new LiteralBitValue(false) }
                    : Enumerable.Repeat(LiteralBitValue.False, length.Value).ToArray();
                return;
            }

            int size = (int)Math.Log(number, 2) + 1;
            if (length != null && length > size)
                size = length.Value;

            var b = new BitValue[size];
            for (int i = 0; i < size; i++)
            {
                b[i] = new LiteralBitValue(((number >> (size - 1 - i)) & 1) == 1);
            }
            this.Values = b;
        }

        public override string ToString() => "(" + string.Join(", ", (object[])Values) + ")";
    }
}
