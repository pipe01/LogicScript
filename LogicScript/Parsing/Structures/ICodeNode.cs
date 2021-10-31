using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal interface ICodeNode : IEnumerable<ICodeNode>
    {
        SourceSpan Span { get; }
    }
}
