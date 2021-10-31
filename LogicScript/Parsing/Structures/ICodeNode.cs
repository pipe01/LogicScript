namespace LogicScript.Parsing.Structures
{
    internal interface ICodeNode
    {
        SourceSpan Span { get; }
    }
}
