using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Reference(SourceSpan span) : ICodeNode
    {
        public abstract IPortInfo Port { get; }

        public abstract bool IsWritable { get; }
        public abstract bool IsReadable { get; }

        public int BitSize => Port.BitSize;

        public SourceSpan Span { get; } = span;

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield return Port;
        }
    }

    internal sealed class PortReference(SourceSpan span, MachinePortInfo port) : Reference(span)
    {
        public MachinePortInfo PortInfo { get; } = port;

        public override IPortInfo Port => PortInfo;

        public int StartIndex => PortInfo.StartIndex;

        public override bool IsWritable => PortInfo.Target is MachinePorts.Output or MachinePorts.Register;
        public override bool IsReadable => PortInfo.Target is MachinePorts.Input or MachinePorts.Register;

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

    internal sealed class LocalReference(SourceSpan span, string name, LocalInfo local) : Reference(span)
    {
        public string Name { get; } = name;
        public LocalInfo LocalInfo { get; } = local;

        public override IPortInfo Port => LocalInfo;

        public ICodeNode Declaration => LocalInfo;

        public override bool IsWritable => true;
        public override bool IsReadable => true;

        public override string ToString() => $"${Name}'{BitSize}";
    }
}
