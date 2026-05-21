using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class WhileStatement(NodeID id, SourceSpan span, Expression condition, Statement body) : Statement(span), IIdentifiableCodeNode
    {
        public NodeID ID { get; } = id;

        public Expression Condition { get; } = condition;
        public Statement Body { get; } = body;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
        }
    }
}
