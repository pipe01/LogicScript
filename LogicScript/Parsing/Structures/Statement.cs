using System;

namespace LogicScript.Parsing.Structures
{
    internal abstract class Statement : ICodeNode
    {
        public SourceLocation Location { get; }

        protected Statement(SourceLocation location)
        {
            this.Location = location;
        }
    }

    internal class OutputSetStatement : Statement
    {
        public Output Output { get; }
        public Expression Value { get; }

        public OutputSetStatement(Output output, Expression value, SourceLocation location) : base(location)
        {
            this.Output = output ?? throw new ArgumentNullException(nameof(output));
            this.Value = value;
        }

        public override string ToString() => $"{Output} = {Value}";
    }
}
