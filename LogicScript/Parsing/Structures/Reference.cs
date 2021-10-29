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
        bool IsWritable { get; }
        bool IsReadable { get; }

        int BitSize { get; }
    }

    internal sealed class PortReference : IReference
    {
        public ReferenceTarget Target { get; }
        public int StartIndex { get; }
        public int BitSize { get; }

        public bool IsWritable => Target is ReferenceTarget.Output or ReferenceTarget.Register;
        public bool IsReadable => Target is ReferenceTarget.Input or ReferenceTarget.Register;

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

    internal sealed class LocalReference : IReference
    {
        public string Name { get; }
        public int BitSize { get; }

        public bool IsWritable => true;
        public bool IsReadable => true;

        public LocalReference(string name, int bitSize)
        {
            this.Name = name;
            this.BitSize = bitSize;
        }

        public override string ToString() => $"${Name}'{BitSize}";
    }
}
