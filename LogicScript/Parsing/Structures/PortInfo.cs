using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal interface IPortInfo : ICodeNode, IEquatable<IPortInfo>
    {
        int BitSize { get; }
    }

    internal enum MachinePorts
    {
        Input,
        Output,
        Register
    }

    internal readonly struct PortInfo : IPortInfo
    {
        public MachinePorts Target { get; }
        public int StartIndex { get; }
        public int BitSize { get; }

        public SourceSpan Span { get; }

        public PortInfo(MachinePorts target, int index, int bitSize, SourceSpan span)
        {
            this.StartIndex = index;
            this.BitSize = bitSize;
            this.Span = span;
            this.Target = target;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override bool Equals(object obj) => obj is IPortInfo other && Equals(other);

        public bool Equals(IPortInfo other)
        {
            return other is PortInfo port
                && port.Target == Target
                && port.StartIndex == StartIndex
                && port.BitSize == BitSize
                && port.Span.Equals(Span);
        }
    }
}
