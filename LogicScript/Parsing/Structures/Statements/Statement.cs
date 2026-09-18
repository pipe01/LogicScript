using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    public abstract class Statement(NodeID id, SourceSpan span) : IIdentifiableCodeNode
    {
        public SourceSpan Span { get; } = span;
        public NodeID ID { get; } = id;

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
