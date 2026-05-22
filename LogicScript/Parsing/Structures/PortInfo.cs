using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public interface IPortInfo : ICodeNode, IEquatable<IPortInfo>
    {
        int BitSize { get; }
    }

    internal enum MachinePorts
    {
        Input,
        Output,
        Register
    }

    internal readonly struct PortInfo(MachinePorts target, int index, int bitSize, SourceSpan span) : IPortInfo
    {
        public MachinePorts Target { get; } = target;
        public int StartIndex { get; } = index;
        public int BitSize { get; } = bitSize;

        public SourceSpan Span { get; } = span;

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override bool Equals(object? obj) => obj is IPortInfo other && Equals(other);

        public bool Equals(IPortInfo? other)
        {
            return other is PortInfo port
                && port.Target == Target
                && port.StartIndex == StartIndex
                && port.BitSize == BitSize
                && port.Span.Equals(Span);
        }

        public override int GetHashCode() => HashCode.Combine(Target, StartIndex, BitSize, Span);
    }
}
