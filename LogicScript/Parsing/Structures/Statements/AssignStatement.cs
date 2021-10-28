using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class AssignStatement : Statement
    {
        public Reference Target { get; set; }
        public Expression Value { get; set; }

        public AssignStatement(SourceLocation location, Reference target, Expression value) : base(location)
        {
            this.Target = target;
            this.Value = value;
        }
    }
}
