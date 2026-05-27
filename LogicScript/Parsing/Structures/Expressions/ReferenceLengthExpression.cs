using System;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceLengthExpression(SourceSpan span, Reference reference) : Expression(span)
    {
        public Reference Reference { get; } = reference;

        public override bool IsConstant => true;
        public override int BitSize => (int)Math.Ceiling(Math.Log(Value, 2));

        public int Value
        {
            get
            {
                if (Reference is PortReference portRef)
                    return portRef.PortInfo.BitSize * portRef.PortInfo.VectorLength;

                return Reference.BitSize;
            }
        }

        public override string ToString() => $"len({Reference})";
    }
}
