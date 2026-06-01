using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression(SourceSpan span, Reference target) : Expression(span)
    {
        public Reference Reference => target;

        public override bool IsConstant => target.IsConstant;
        public override int BitSize => Reference.BitSize;

        public override string ToString() => Reference.ToString() ?? "<unknown reference>";

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Reference;
        }
    }
}
