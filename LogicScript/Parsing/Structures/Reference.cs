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

        public Reference(ReferenceTarget target, string name)
        {
            this.Target = target;
            this.Name = name;
        }
    }
}
