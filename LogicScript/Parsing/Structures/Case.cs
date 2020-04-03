using System;

namespace LogicScript.Parsing.Structures
{
    internal class Case : ICodeNode
    {
        public Expression Condition { get; }
        public Statement[] Statements { get; }
        public SourceLocation Location { get; }

        public Case(Expression condition, Statement[] statements, SourceLocation location)
        {
            this.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            this.Statements = statements;
            this.Location = location;
        }
    }
}
