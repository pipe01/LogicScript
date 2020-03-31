using System;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Statement
    {
    }

    internal class OutputSetStatement : Statement
    {
        public Output Output { get; }
        public BitsValue Value { get; }

        public OutputSetStatement(Output output, BitsValue value)
        {
            this.Output = output ?? throw new ArgumentNullException(nameof(output));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override string ToString() => $"{Output} = {Value}";
    }
}
