using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal enum IndexStart
    {
        Left,
        Right,
    }

    internal sealed class SliceExpression : Expression
    {
        public Expression Operand { get; set; }
        public IndexStart Start { get; set; }
        public Expression Offset { get; set; }
        public int Length { get; set; }

        public override bool IsConstant => Operand.IsConstant;

        public override int BitSize => Length;

        public SliceExpression(SourceSpan span, Expression operand, IndexStart start, Expression offset, int length) : base(span)
        {
            this.Operand = operand;
            this.Offset = offset;
            this.Length = length;
            this.Start = start;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
            yield return Offset;
        }
    }
}
