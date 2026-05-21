namespace LogicScript.Parsing.Structures.Statements
{
    internal class BreakStatement : Statement
    {
        public NodeID TargetID { get; }

        public BreakStatement(SourceSpan span, NodeID targetID) : base(span)
        {
            this.TargetID = targetID;
        }
    }
}
