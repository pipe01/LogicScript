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
        public SlotExpression Slot { get; }
        public Expression Value { get; }

        public AssignStatement(SlotExpression slot, Expression value, SourceLocation location) : base(location)
        {
            this.Value = value;
            this.Slot = slot;
        }

        public override string ToString() => $"{Slot} = {Value}";
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
}
