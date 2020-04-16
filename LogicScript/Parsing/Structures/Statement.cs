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

    internal class AssignStatement : Statement
    {
        public Expression LeftSide { get; }
        public Expression RightSide { get; }

        public AssignStatement(Expression leftSide, Expression rightSide, SourceLocation location) : base(location)
        {
            this.LeftSide = leftSide;
            this.RightSide = rightSide;
        }

        public override string ToString() => $"{LeftSide} = {RightSide}";
    }

    internal class IfStatement : Statement
    {
        public Expression Condition { get; }
        public IReadOnlyList<Statement> Body { get; }
        public IReadOnlyList<Statement> Else { get; }

        public IfStatement(Expression condition, IReadOnlyList<Statement> body, IReadOnlyList<Statement> @else, SourceLocation location) : base(location)
        {
            this.Condition = condition;
            this.Body = body;
            this.Else = @else;
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
}
