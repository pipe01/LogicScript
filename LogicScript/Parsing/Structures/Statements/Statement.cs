using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    public abstract class Statement(NodeID id, SourceSpan span) : ICodeNode, IIdentifiableCodeNode
    {
        public NodeID ID { get; } = id;
        public SourceSpan Span { get; } = span;

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }
}
