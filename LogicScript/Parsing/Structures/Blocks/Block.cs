using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal abstract class Block(SourceSpan span) : ICodeNode
    {
        public SourceSpan Span { get; } = span;

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
