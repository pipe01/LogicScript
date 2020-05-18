using System;

namespace LogicScript.Parsing.Structures
{
    internal class RangeExpression : Expression
    {
        public override bool IsSingleBit => false;
        public override ExpressionType Type => ExpressionType.Indexer;

        public override bool IsReadable => Operand.IsReadable;
        public override bool IsWriteable => Operand.IsWriteable;

        public Expression Operand { get; set; }
        public Index Start { get; set; }
        public Index End { get; set; }

        public RangeExpression(Expression operand, Index start, Index end, SourceLocation location) : base(location)
        {
            this.Operand = operand ?? throw new ArgumentNullException(nameof(operand));
            this.Start = start;
            this.End = end;
        }

        public override string ToString() => $"{Operand}[{Start}..{End}]";
    }
}
