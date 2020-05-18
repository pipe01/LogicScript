using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Statement : ICodeNode
    {
        public SourceLocation Location { get; }

        protected Statement(SourceLocation location)
        {
            this.Location = location;
        }
    }

    internal class ExpressionStatement : Statement
    {
        public Expression Expression { get; set; }

        public ExpressionStatement(Expression expression, SourceLocation location) : base(location)
        {
            this.Expression = expression;
        }

        public override string ToString() => Expression.ToString();
    }

    internal class IfStatement : Statement
    {
        public Expression Condition { get; set; }
        public IReadOnlyList<Statement> Body { get; set; }
        public IReadOnlyList<Statement> Else { get; set; }

        public IfStatement(Expression condition, IReadOnlyList<Statement> body, IReadOnlyList<Statement> @else, SourceLocation location) : base(location)
        {
            this.Condition = condition;
            this.Body = body;
            this.Else = @else ?? Array.Empty<Statement>();
        }

        public override string ToString() => $"if {Condition}";
    }

    internal class QueueUpdateStatement : Statement
    {
        public QueueUpdateStatement(SourceLocation location) : base(location)
        {
        }

        public override string ToString() => "update";
    }

    internal class BreakStatement : Statement
    {
        public BreakStatement(SourceLocation location) : base(location)
        {
        }

        public override string ToString() => "break";
    }

    internal class ForStatement : Statement
    {
        public string VarName { get; set; }
        public Expression From { get; set; }
        public Expression To { get; set; }
        public IReadOnlyList<Statement> Body { get; set; }

        public ForStatement(string varName, Expression from, Expression to, IReadOnlyList<Statement> body, SourceLocation location) : base(location)
        {
            this.From = from;
            this.To = to;
            this.Body = body;
            this.VarName = varName;
        }
    }

    internal class WhileStatement : Statement
    {
        public Expression Condition { get; set; }
        public IReadOnlyList<Statement> Body { get; set; }

        public WhileStatement(Expression condition, IReadOnlyList<Statement> body, SourceLocation location) : base(location)
        {
            this.Condition = condition;
            this.Body = body;
        }
    }
}
