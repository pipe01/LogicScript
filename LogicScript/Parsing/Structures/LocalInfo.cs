using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal readonly struct LocalInfo : ICodeNode
    {
        public int BitSize { get; }

        public SourceSpan Span { get; }

        public LocalInfo(int bitSize, SourceSpan span)
        {
            this.BitSize = bitSize;
            this.Span = span;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
