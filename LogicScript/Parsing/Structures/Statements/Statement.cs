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

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
