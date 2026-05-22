using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class TruncateExpression(SourceSpan span, Expression operand, int size) : Expression(span)
    {
        public Expression Operand { get; set; } = operand;
        public int Size { get; set; } = size;

        public override bool IsConstant => Operand.IsConstant;
        public override int BitSize => Size;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
        }
    }
}
