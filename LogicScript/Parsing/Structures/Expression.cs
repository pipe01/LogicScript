namespace LogicScript.Parsing.Structures
{
    internal abstract class Expression : ICodeNode
    {
        public abstract bool IsSingleBit { get; }

        public SourceLocation Location { get; }

        protected Expression(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
