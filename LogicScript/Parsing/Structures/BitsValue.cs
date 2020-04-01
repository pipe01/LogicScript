using System;
using System.Linq;

namespace LogicScript.Parsing.Structures
{
    internal class BitsValue : Expression
    {
        /// <summary>
        /// Big endian
        /// </summary>
        public BitExpression[] Values { get; }

        public BitsValue(BitExpression[] values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public BitsValue(uint number, int? length = null)
        {
            if (number == 0)
            {
                this.Values = length == null
                    ? new[] { new LiteralBitExpression(false) }
                    : Enumerable.Repeat(LiteralBitExpression.False, length.Value).ToArray();
                return;
            }

            int size = (int)Math.Log(number, 2) + 1;
            if (length != null && length > size)
                size = length.Value;

            var b = new BitExpression[size];
            for (int i = 0; i < size; i++)
            {
                b[i] = new LiteralBitExpression(((number >> (size - 1 - i)) & 1) == 1);
            }
            this.Values = b;
        }

        public override string ToString() => "(" + string.Join(", ", (object[])Values) + ")";
    }
}
