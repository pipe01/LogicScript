using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class ForStatement : Statement
    {
        public string VariableName { get; set; }
        public Expression? From { get; set; }
        public Expression To { get; set; }

        public Statement Body { get; set; }

        public ForStatement(SourceLocation location, string variableName, Expression? from, Expression to, Statement body) : base(location)
        {
            this.VariableName = variableName;
            this.From = from;
            this.To = to;
            this.Body = body;
        }
    }
}
