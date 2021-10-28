namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class Statement : ICodeNode
    {
        public SourceLocation Location { get; }

        protected Statement(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
