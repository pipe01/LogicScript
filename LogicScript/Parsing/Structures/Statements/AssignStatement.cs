using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement(SourceSpan span, Reference target, Expression value) : Statement(span)
    {
        public Reference Reference { get; } = target;
        public Expression Value { get; } = value;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Reference;
            yield return Value;
        }
    }
}
