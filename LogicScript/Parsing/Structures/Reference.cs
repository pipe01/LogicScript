using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Reference : ICodeNode
    {
        public abstract IPortInfo Port { get; }

        public abstract bool IsWritable { get; }
        public abstract bool IsReadable { get; }

        public int BitSize => Port.BitSize;

        public SourceSpan Span { get; }

        protected Reference(SourceSpan span)
        {
            this.Span = span;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }

    internal sealed class PortReference : Reference
    {
        public PortInfo PortInfo { get; }

        public override IPortInfo Port => PortInfo;

        public int StartIndex => PortInfo.StartIndex;
        public int BitSize => PortInfo.BitSize;

        public override bool IsWritable => PortInfo.Target is MachinePorts.Output or MachinePorts.Register;
        public override bool IsReadable => PortInfo.Target is MachinePorts.Input or MachinePorts.Register;

        public PortReference(SourceSpan span, PortInfo port) : base(span)
        {
            this.PortInfo = port;
        }

        public override string ToString()
        {
            var target = PortInfo.Target switch
            {
                MachinePorts.Input => "input",
                MachinePorts.Output => "output",
                MachinePorts.Register => "reg",
                _ => throw new System.Exception("Unknown target")
            };

            return $"{target}[{StartIndex}..{StartIndex + BitSize}]";
        }
    }

    internal sealed class LocalReference : Reference
    {
        public string Name { get; }
        public LocalInfo LocalInfo { get; }

        public override IPortInfo Port => LocalInfo;

        public ICodeNode Declaration => LocalInfo;
        public int BitSize => LocalInfo.BitSize;

        public override bool IsWritable => true;
        public override bool IsReadable => true;

        public LocalReference(SourceSpan span, string name, LocalInfo local) : base(span)
        {
            this.Name = name;
            this.LocalInfo = local;
        }

        public override string ToString() => $"${Name}'{BitSize}";
    }
}
