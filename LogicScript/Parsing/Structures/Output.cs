namespace LogicScript.Parsing.Structures
{
    internal class Output
    {
        public int? Index { get; }
        public bool IsIndexed { get; }

        public Output(int? index)
        {
            this.Index = index;
            this.IsIndexed = index != null;
        }

        public override string ToString() => $"out[{Index}]";
    }
}
