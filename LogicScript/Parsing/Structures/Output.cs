namespace LogicScript.Parsing.Structures
{
    internal class Output
    {
        public int? Index { get; }

        public Output(int? index)
        {
            this.Index = index;
        }

        public override string ToString() => $"out[{Index}]";
    }
}
