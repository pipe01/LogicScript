namespace LogicScript.Parsing.Structures
{
    internal abstract class Expression : ICodeNode
    {
        public abstract bool IsSingleBit { get; }

        public virtual bool IsReadable => false;
        public virtual bool IsWriteable => false;

        public SourceLocation Location { get; }

        protected Expression(SourceLocation location)
        {
            this.Location = location;
        }
    }
}
