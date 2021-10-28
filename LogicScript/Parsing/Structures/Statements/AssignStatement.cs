using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement : Statement
    {
        public Reference Reference { get; set; }
        public Expression Value { get; set; }

        public AssignStatement(SourceLocation location, Reference target, Expression value) : base(location)
        {
            this.Reference = target;
            this.Value = value;
        }
    }
}
