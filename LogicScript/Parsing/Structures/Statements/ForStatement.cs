using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class ForStatement(NodeID id, SourceSpan span, LocalInfo variable, Expression? from, Expression to, Statement body) : Statement(span), IIdentifiableCodeNode
    {
        public NodeID ID { get; } = id;

        public LocalInfo Variable { get; set; } = variable;
        public Expression? From { get; set; } = from;
        public Expression To { get; set; } = to;

        public Statement Body { get; set; } = body;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            if (From != null)
                yield return From;
            yield return To;
            yield return Body;
        }
    }
}
