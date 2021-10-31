using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal interface ICodeNode
    {
        SourceSpan Span { get; }

        IEnumerable<ICodeNode> GetChildren();
    }
}
