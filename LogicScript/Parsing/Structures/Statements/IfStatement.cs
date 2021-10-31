using LogicScript.Parsing.Structures.Expressions;

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
    }
}
