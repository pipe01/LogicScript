using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement : Statement
    {
        public Reference Reference { get; set; }
        public Expression Value { get; set; }

        public AssignStatement(SourceSpan span, Reference target, Expression value) : base(span)
        {
            this.Reference = target;
            this.Value = value;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }
}
