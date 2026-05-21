using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class ForStatement : Statement, IIdentifiableCodeNode
    {
        public NodeID ID { get; }

        public LocalInfo Variable { get; set; }
        public Expression? From { get; set; }
        public Expression To { get; set; }

        public Statement Body { get; set; }

        public ForStatement(NodeID id, SourceSpan span, LocalInfo variable, Expression? from, Expression to, Statement body) : base(span)
        {
            this.ID = id;
            this.Variable = variable;
            this.From = from;
            this.To = to;
            this.Body = body;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            if (From != null)
                yield return From;
            yield return To;
            yield return Body;
        }
    }
}
