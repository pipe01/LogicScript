using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    public abstract class Statement(SourceSpan span) : ICodeNode
    {
        public SourceSpan Span { get; } = span;

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
