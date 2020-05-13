using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal class FunctionCallExpression : Expression
    {
        public override bool IsSingleBit => Name == "and" || Name == "or";
        public override ExpressionType Type => ExpressionType.FunctionCall;

        public override bool IsReadable => true;

        public string Name { get; set; }
        public IList<Expression> Arguments { get; set; }

        public FunctionCallExpression(string name, IList<Expression> arguments, SourceLocation location) : base(location)
        {
            this.Name = name;
            this.Arguments = arguments;
        }

        public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";
    }
}
