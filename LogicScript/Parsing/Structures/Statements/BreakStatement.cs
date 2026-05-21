namespace LogicScript.Parsing.Structures.Statements
{
    internal class BreakStatement(SourceSpan span, NodeID targetID) : Statement(span)
    {
        public NodeID TargetID { get; } = targetID;
    }
}
