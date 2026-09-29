using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal abstract class Expression(SourceSpan span) : ICodeNode
    {
        public SourceSpan Span { get; } = span;

        public abstract bool IsConstant { get; }
        public abstract int BitSize { get; }

        public virtual Integer ResultType => BitSize.ToIntegerSize();

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
