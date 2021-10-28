namespace LogicScript.Parsing.Structures.Expressions
{
    internal abstract class Expression
    {
        public SourceLocation Location { get; }

        public Expression(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
