using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Case : ICodeNode
    {
        public IReadOnlyList<Statement> Statements { get; }
        public SourceLocation Location { get; }

        public Case(IReadOnlyList<Statement> statements, SourceLocation location)
        {
            this.Statements = statements;
            this.Location = location;
        }
    }

    internal class ConditionalCase : Case
    {
        public Expression Condition { get; }

        public ConditionalCase(Expression condition, IReadOnlyList<Statement> statements, SourceLocation location) : base(statements, location)
        {
            this.Condition = condition;
        }
    }

    internal class UnconditionalCase : Case
    {
        public UnconditionalCase(IReadOnlyList<Statement> statements, SourceLocation location) : base(statements, location)
        {
        }
    }

    internal class OnceCase : Case
    {
        public OnceCase(IReadOnlyList<Statement> statements, SourceLocation location) : base(statements, location)
        {
        }
    }
}
