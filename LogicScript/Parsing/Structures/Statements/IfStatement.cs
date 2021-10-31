using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public Statement Body { get; set; }
        public Statement? Else { get; set; }

        public IfStatement(SourceSpan span, Expression condition, Statement body, Statement? @else) : base(span)
        {
            this.Condition = condition;
            this.Body = body;
            this.Else = @else;
        }

        protected override IEnumerator<ICodeNode> GetChildren()
        {
            yield return Condition;
            yield return Body;
            if (Else != null)
                yield return Else;
        }
    }
}
