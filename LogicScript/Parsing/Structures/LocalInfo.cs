using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal readonly struct LocalInfo : IPortInfo
    {
        public int BitSize { get; }
        public string Name { get; }
        public SourceSpan Span { get; }

        public LocalInfo(int bitSize, string name, SourceSpan span)
        {
            this.BitSize = bitSize;
            this.Name = name;
            this.Span = span;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override bool Equals(object obj) => obj is IPortInfo other && Equals(other);

        public bool Equals(IPortInfo other)
        {
            return other is LocalInfo local
                && local.Name == Name
                && local.BitSize == BitSize
                && local.Span.Equals(Span);
        }
    }
}
