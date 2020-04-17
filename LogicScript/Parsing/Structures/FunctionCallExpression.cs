using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal class FunctionCallExpression : Expression
    {
        public override bool IsSingleBit => Name == "and" || Name == "or";

        public override bool IsReadable => true;

        public string Name { get; }
        public IReadOnlyList<Expression> Arguments { get; }

        public FunctionCallExpression(string name, IReadOnlyList<Expression> arguments, SourceLocation location) : base(location)
        {
            this.Name = name;
            this.Arguments = arguments;
        }

        public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";
    }
}
