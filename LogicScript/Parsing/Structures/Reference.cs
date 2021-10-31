using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures
{
    internal enum ReferenceTarget
    {
        Input,
        Output,
        Register
    }

    internal interface IReference
    {
        ICodeNode Declaration { get; }

        bool IsWritable { get; }
        bool IsReadable { get; }

        int BitSize { get; }
    }

    internal sealed class PortReference : IReference
    {
        public ReferenceTarget Target { get; }
        public PortInfo Port { get; }

        public int StartIndex => Port.StartIndex;
        public int BitSize => Port.BitSize;

        public ICodeNode Declaration => Port;

        public bool IsWritable => Target is ReferenceTarget.Output or ReferenceTarget.Register;
        public bool IsReadable => Target is ReferenceTarget.Input or ReferenceTarget.Register;

        public PortReference(ReferenceTarget target, PortInfo port)
        {
            this.Target = target;
            this.Port = port;
        }

        public override string ToString()
        {
            var target = Target switch
            {
                ReferenceTarget.Input => "input",
                ReferenceTarget.Output => "output",
                ReferenceTarget.Register => "reg",
                _ => throw new System.Exception("Unkonwn target")
            };

            return $"{target}[{StartIndex}..{StartIndex + BitSize}]";
        }
    }

    internal sealed class LocalReference : IReference
    {
        public string Name { get; }
        public LocalInfo Local { get; }

        public ICodeNode Declaration => Local;
        public int BitSize => Local.BitSize;

        public bool IsWritable => true;
        public bool IsReadable => true;

        public LocalReference(string name, LocalInfo local)
        {
            this.Name = name;
            this.Local = local;
        }

        public override string ToString() => $"${Name}'{BitSize}";
    }
}
