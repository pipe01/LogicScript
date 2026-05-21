using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public interface ICodeNode
    {
        SourceSpan Span { get; }

        IEnumerable<ICodeNode> GetChildren();
    }
}
