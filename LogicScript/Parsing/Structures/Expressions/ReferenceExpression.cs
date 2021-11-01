using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression : Expression
    {
        public Reference Reference { get; set; }

        public override bool IsConstant => false;
        public override int BitSize => Reference.BitSize;

        public ReferenceExpression(SourceSpan span, Reference target) : base(span)
        {
            this.Reference = target;
        }

        public override string ToString() => Reference.ToString();

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Reference;
        }
    }
}
