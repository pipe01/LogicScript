using System.Collections.Generic;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class ReturnStatement(NodeID id, SourceSpan span, Expression value) : Statement(id, span)
    {
        public Expression Value { get; } = value;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }
}
