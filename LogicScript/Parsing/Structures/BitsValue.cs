using System;

namespace LogicScript.Parsing.Structures
{
    internal abstract class BitsValue
    {
    }

    internal class LiteralBitsValue : BitsValue
    {
        public uint Number { get; }

        public LiteralBitsValue(uint number)
        {
            this.Number = number;
        }

        public override string ToString() => $"{Number}'";
    }

    internal class CompoundBitsValue : BitsValue
    {
        public BitValue[] Values { get; }

        public CompoundBitsValue(BitValue[] values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public override string ToString() => "(" + string.Join(", ", (object[])Values) + ")";
    }
}
