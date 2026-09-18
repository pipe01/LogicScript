using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class IfStatement(NodeID id, SourceSpan span, Expression condition, Statement body, Statement? @else) : Statement(id, span)
    {
        public Expression Condition { get; set; } = condition;
        public Statement Body { get; set; } = body;
        public Statement? Else { get; set; } = @else;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
            if (Else != null)
                yield return Else;
        }
    }
}
