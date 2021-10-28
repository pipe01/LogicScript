namespace LogicScript.Parsing.Structures
{
    internal enum ReferenceTarget
    {
        Input,
        Output,
        Register
    }

    internal abstract class Reference
    {
        public abstract bool IsWritable { get; }
        public abstract bool IsReadable { get; }

        public abstract int BitSize { get; }
    }

    internal sealed class PortReference : Reference
    {
        public ReferenceTarget Target { get; }
        public int StartIndex { get; }
        public override int BitSize { get; }

        public override bool IsWritable => Target is ReferenceTarget.Output or ReferenceTarget.Register;
        public override bool IsReadable => Target is ReferenceTarget.Input or ReferenceTarget.Register;

        public PortReference(ReferenceTarget target, int startIndex, int length)
        {
            this.Target = target;
            this.StartIndex = startIndex;
            this.BitSize = length;
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

    internal sealed class LocalReference : Reference
    {
        public string Name { get; }
        public override int BitSize { get; }

        public override bool IsWritable => true;
        public override bool IsReadable => true;

        public LocalReference(string name, int bitSize)
        {
            this.Name = name;
            this.BitSize = bitSize;
        }

        public override string ToString() => $"${Name}'{BitSize}";
    }
}
