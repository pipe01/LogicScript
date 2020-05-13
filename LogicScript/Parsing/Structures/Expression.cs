namespace LogicScript.Parsing.Structures
{
    internal abstract class Expression : ICodeNode
    {
        public abstract bool IsSingleBit { get; }
        public abstract ExpressionType Type { get; }

        public virtual bool IsReadable => false;
        public virtual bool IsWriteable => false;

        public virtual bool IsConstant => false;

        public SourceLocation Location { get; }

        protected Expression(SourceLocation location)
        {
            this.Location = location;
        }
    }

    internal enum ExpressionType
    {
        FunctionCall,
        Indexer,
        List,
        NumberLiteral,
        Operator,
        Slot,
        UnaryOperator,
        VariableAccess,
    }
}
