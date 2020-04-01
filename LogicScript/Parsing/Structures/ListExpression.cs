using System;

namespace LogicScript.Parsing.Structures
{
    internal class ListExpression : Expression
    {
        public Expression[] Expressions { get; }

        public ListExpression(Expression[] expressions)
        {
            this.Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }
    }
}
