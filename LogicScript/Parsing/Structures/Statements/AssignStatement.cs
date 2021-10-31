using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement : Statement
    {
        public IReference Reference { get; set; }
        public Expression Value { get; set; }

        public AssignStatement(SourceSpan span, IReference target, Expression value) : base(span)
        {
            this.Reference = target;
            this.Value = value;
        }

        protected override IEnumerator<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }
}
