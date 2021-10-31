using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class Statement : ICodeNode
    {
        public SourceSpan Span { get; }

        protected Statement(SourceSpan span)
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
