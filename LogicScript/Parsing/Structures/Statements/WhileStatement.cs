using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class WhileStatement : Statement, IIdentifiableCodeNode
    {
        public NodeID ID { get; }

        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(NodeID id, SourceSpan span, Expression condition, Statement body) : base(span)
        {
            this.ID = id;
            this.Condition = condition;
            this.Body = body;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
        }
    }
}
