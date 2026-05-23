using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public readonly struct LocalInfo(NodeID id, int bitSize, string name, SourceSpan span) : IPortInfo, IIdentifiableCodeNode
    {
        public int BitSize { get; } = bitSize;
        public string Name { get; } = name;
        public SourceSpan Span { get; } = span;

        public NodeID ID { get; } = id;

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override bool Equals(object? obj) => obj is IPortInfo other && Equals(other);

        public bool Equals(IPortInfo? other)
        {
            return other is LocalInfo local
                && local.Name == Name
                && local.BitSize == BitSize;
        }

        public override int GetHashCode() => HashCode.Combine(BitSize, Name, Span);

        public override string ToString() => Name;
    }
}
