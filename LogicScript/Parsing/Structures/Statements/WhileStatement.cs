using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public Statement Body { get; }

        public WhileStatement(SourceSpan span, Expression condition, Statement body) : base(span)
        {
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
