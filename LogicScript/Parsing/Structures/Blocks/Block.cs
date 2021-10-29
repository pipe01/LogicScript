namespace LogicScript.Parsing.Structures.Blocks
{
    internal abstract class Block : ICodeNode
    {
        public SourceLocation Location { get; }

        protected Block(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
