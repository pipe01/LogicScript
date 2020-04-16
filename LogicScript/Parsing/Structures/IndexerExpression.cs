using LogicScript.Data;
using System;

namespace LogicScript.Parsing.Structures
{
    internal class IndexerExpression : Expression
    {
        public override bool IsSingleBit => Range.Length == 1;

        public override bool IsReadable => Operand.IsReadable;
        public override bool IsWriteable => Operand.IsWriteable;

        public Expression Operand { get; }
        public BitRange Range { get; }

        public IndexerExpression(Expression operand, BitRange range, SourceLocation location) : base(location)
        {
            this.Operand = operand ?? throw new ArgumentNullException(nameof(operand));
            this.Range = range;
        }

        public override string ToString() => $"{Operand}[{Range}]";
    }
}
