using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal enum IndexStart
    {
        Left,
        Right,
    }

    internal sealed class SliceExpression(SourceSpan span, Expression operand, IndexStart start, Expression offset, int length) : Expression(span)
    {
        public Expression Operand { get; set; } = operand;
        public IndexStart Start { get; set; } = start;
        public Expression Offset { get; set; } = offset;
        public int Length { get; set; } = length;

        public override bool IsConstant => Operand.IsConstant;

        public override int BitSize => Length;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
            yield return Offset;
        }
    }
}
