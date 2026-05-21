using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public readonly struct LocalInfo(int bitSize, string name, string originalName, SourceSpan span) : IPortInfo
    {
        public int BitSize { get; } = bitSize;
        public string Name { get; } = name;
        public string OriginalName { get; } = originalName;
        public SourceSpan Span { get; } = span;

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

        public override int GetHashCode() => HashCode.Combine(BitSize, Name);

        public override string ToString() => $"{Name}'{BitSize}";
    }
}
