using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class DeclareLocalStatement : Statement
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public Expression? Initializer { get; set; }

        public DeclareLocalStatement(SourceLocation location, string name, int size, Expression? initializer) : base(location)
        {
            this.Name = name;
            this.Size = size;
            this.Initializer = initializer;
        }
    }
}
