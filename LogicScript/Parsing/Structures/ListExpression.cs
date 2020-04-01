using System;

namespace LogicScript.Parsing.Structures
{
    internal class ListExpression : Expression
    {
        public Expression[] Expressions { get; }
        public override bool IsSingleBit => Expressions.Length == 1;

        public ListExpression(Expression[] expressions, SourceLocation location) : base(location)
        {
            this.Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }

        public override string ToString() => "(" + string.Join<Expression>(", ", Expressions) + ")";
    }
}
