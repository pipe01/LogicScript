using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal abstract class Block : ICodeNode
    {
        public SourceSpan Span { get; }

        protected Block(SourceSpan span)
        {
            this.Span = span;
        }

        protected virtual IEnumerator<ICodeNode> GetChildren()
        {
            yield break;
        }

        IEnumerator<ICodeNode> IEnumerable<ICodeNode>.GetEnumerator() => GetChildren();

        IEnumerator IEnumerable.GetEnumerator() => GetChildren();
    }
}
