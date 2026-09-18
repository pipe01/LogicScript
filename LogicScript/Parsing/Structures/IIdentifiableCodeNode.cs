namespace LogicScript.Parsing.Structures
{
    internal interface IIdentifiableCodeNode : ICodeNode
    {
        NodeID ID { get; }
    }
}
