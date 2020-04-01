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

    internal class SetOutputStatement : Statement
    {
        public Expression Value { get; }

        public SetOutputStatement(Expression value, SourceLocation location) : base(location)
        {
            this.Value = value;
        }

        public override string ToString() => $"out = {Value}";
    }

    internal class SetSingleOutputStatement : SetOutputStatement
    {
        public int Output { get; }

        public SetSingleOutputStatement(int output, Expression value, SourceLocation location) : base(value, location)
        {
            this.Output = output;
        }

        public override string ToString() => $"out[{Output}] = {Value}";
    }
}
