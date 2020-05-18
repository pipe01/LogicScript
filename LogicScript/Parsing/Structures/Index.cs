using System;

namespace LogicScript.Parsing.Structures
{
    internal readonly struct Index
    {
        public static readonly Index End = new Index(null, true);

        public Expression Value { get; }
        public bool FromEnd { get; }

        public Index(Expression value, bool fromEnd)
        {
            if (!fromEnd && value == null)
                throw new ArgumentNullException(nameof(value));

            this.Value = value;
            this.FromEnd = fromEnd;
        }

        public override string ToString() => (FromEnd ? "^" : "") + Value;

        public override bool Equals(object obj)
        {
            if (!(obj is Index other))
            {
                return false;
            }

            return FromEnd == other.FromEnd && Value == other.Value;
        }

        public override int GetHashCode() => Value.GetHashCode() + (17 * FromEnd.GetHashCode());

        public static bool operator ==(Index a, Index b) => a.Equals(b);
        public static bool operator !=(Index a, Index b) => !a.Equals(b);
    }
}
