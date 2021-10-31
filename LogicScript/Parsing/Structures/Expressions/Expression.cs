using System.Collections;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal abstract class Expression : ICodeNode
    {
        public SourceSpan Span { get; }

        public abstract bool IsConstant { get; }
        public abstract int BitSize { get; }

        public Expression(SourceSpan span)
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
