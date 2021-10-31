using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression : Expression
    {
        public IReference Reference { get; set; }

        public override bool IsConstant => false;
        public override int BitSize => Reference.BitSize;

        public ReferenceExpression(SourceSpan span, IReference target) : base(span)
        {
            this.Reference = target;
        }

        public override string ToString() => Reference.ToString();
    }
}
