using System;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Case : ICodeNode
    {
        public Statement[] Statements { get; }
        public SourceLocation Location { get; }

        public Case(Statement[] statements, SourceLocation location)
        {
            this.Statements = statements;
            this.Location = location;
        }
    }

    internal class ConditionalCase : Case
    {
        public Expression Condition { get; }

        public ConditionalCase(Expression condition, Statement[] statements, SourceLocation location) : base(statements, location)
        {
            this.Condition = condition;
        }
    }

    internal class UnconditionalCase : Case
    {
        public UnconditionalCase(Statement[] statements, SourceLocation location) : base(statements, location)
        {
        }
    }

    internal class OnceCase : Case
    {
        public OnceCase(Statement[] statements, SourceLocation location) : base(statements, location)
        {
        }
    }
}
