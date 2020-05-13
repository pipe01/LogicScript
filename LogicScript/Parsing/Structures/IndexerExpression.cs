using System;

namespace LogicScript.Parsing.Structures
{
    internal class IndexerExpression : Expression
    {
        public override bool IsSingleBit => End == null;

        public override bool IsReadable => Operand.IsReadable;
        public override bool IsWriteable => Operand.IsWriteable;

        public Expression Operand { get; set; }
        public Expression Start { get; set; }
        public Expression End { get; set; }
        /// <summary>
        /// If false the range will span to the end of the operand.
        /// </summary>
        public bool HasEnd { get; set; }

        public IndexerExpression(Expression operand, Expression start, Expression end, bool hasEnd, SourceLocation location) : base(location)
        {
            this.Operand = operand ?? throw new ArgumentNullException(nameof(operand));
            this.Start = start;
            this.End = end;
            this.HasEnd = hasEnd;
        }

        public override string ToString() => $"{Operand}[{Start},{End}]";
    }
}
