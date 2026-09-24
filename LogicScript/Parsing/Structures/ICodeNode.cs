using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public interface ICodeNode
    {
        SourceSpan Span { get; }

        IEnumerable<ICodeNode> GetChildren();
    }

    internal interface IIdentifiableCodeNode : ICodeNode
    {
        NodeID ID { get; }
    }

    internal interface IHasNameSpan : ICodeNode
    {
        SourceSpan NameSpan { get; }
    }
}
