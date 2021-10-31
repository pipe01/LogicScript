using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal readonly struct PortInfo : ICodeNode
    {
        public int StartIndex { get; }
        public int BitSize { get; }

        public SourceSpan Span { get; }

        public PortInfo(int index, int bitSize, SourceSpan span)
        {
            this.StartIndex = index;
            this.BitSize = bitSize;
            this.Span = span;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
