namespace LogicScript.Parsing.Structures
{
    internal enum ReferenceTarget
    {
        Input,
        Output,
        Register
    }

    internal readonly struct Reference
    {
        public ReferenceTarget Target { get; }
        public int StartIndex { get; }
        public int Length { get; }

        public bool IsWritable => Target is ReferenceTarget.Output or ReferenceTarget.Register;
        public bool IsReadable => Target is ReferenceTarget.Input or ReferenceTarget.Register;

        public Reference(ReferenceTarget target, int startIndex, int length)
        {
            this.Target = target;
            this.StartIndex = startIndex;
            this.Length = length;
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

            return $"{target}[{StartIndex}..{StartIndex + Length}]";
        }
    }
}
