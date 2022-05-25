using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement : Statement
    {
        public Reference Reference { get; }
        public Expression Value { get; }

        public AssignStatement(SourceSpan span, Reference target, Expression value) : base(span)
        {
            this.Reference = target;
            this.Value = value;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Reference;
            yield return Value;
        }
    }
}
