namespace LogicScript.Parsing.Structures.Statements
{
    internal class BreakStatement(NodeID id, SourceSpan span, NodeID targetID) : Statement(id, span)
    {
        public NodeID TargetID { get; } = targetID;
    }
}
