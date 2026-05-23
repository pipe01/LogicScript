using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public interface IPortInfo : ICodeNode, IEquatable<IPortInfo>
    {
        int BitSize { get; }
    }

    public enum MachinePorts
    {
        Input,
        Output,
        Register
    }

    public readonly struct PortInfo : IPortInfo
    {
        public MachinePorts Target { get; }
        public int StartIndex { get; }
        public int BitSize { get; }

        public SourceSpan Span { get; }

        internal PortInfo(MachinePorts target, int index, int bitSize, SourceSpan span)
        {
            this.Target = target;
            this.StartIndex = index;
            this.BitSize = bitSize;
            this.Span = span;
        }

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
