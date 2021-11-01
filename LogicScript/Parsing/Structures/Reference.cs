namespace LogicScript.Parsing.Structures
{
    internal interface IReference
    {
        IPortInfo Port { get; }

        bool IsWritable { get; }
        bool IsReadable { get; }

        int BitSize { get; }
    }

    internal sealed class PortReference : IReference
    {
        public PortInfo Port { get; }

        IPortInfo IReference.Port => Port;

        public int StartIndex => Port.StartIndex;
        public int BitSize => Port.BitSize;

        public ICodeNode Declaration => Port;

        public bool IsWritable => Port.Target is MachinePorts.Output or MachinePorts.Register;
        public bool IsReadable => Port.Target is MachinePorts.Input or MachinePorts.Register;

        public PortReference(PortInfo port)
        {
            this.Port = port;
        }

        public override string ToString()
        {
            var target = Port.Target switch
            {
                MachinePorts.Input => "input",
                MachinePorts.Output => "output",
                MachinePorts.Register => "reg",
                _ => throw new System.Exception("Unkonwn target")
            };

            return $"{target}[{StartIndex}..{StartIndex + BitSize}]";
        }
    }

    internal sealed class LocalReference : IReference
    {
        public string Name { get; }
        public LocalInfo Local { get; }

        IPortInfo IReference.Port => Local;

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
