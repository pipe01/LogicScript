namespace LogicScript.Parsing.Structures
{
    internal abstract class TopLevelNode : ICodeNode
    {
        public SourceLocation Location { get; }

        protected TopLevelNode(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
