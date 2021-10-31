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

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
