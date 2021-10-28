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
        public string Name { get; }

        public bool IsWritable => Target is ReferenceTarget.Output or ReferenceTarget.Register;
        public bool IsReadable => Target is ReferenceTarget.Input or ReferenceTarget.Register;

        public Reference(ReferenceTarget target, string name)
        {
            this.Target = target;
            this.Name = name;
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

            return $"{target} {Name}";
        }
    }
}
