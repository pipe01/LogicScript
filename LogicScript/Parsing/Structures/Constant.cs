using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures
{
    public readonly struct Constant : ICodeNode
    {
        public BitsValue Value { get; }
        internal Expression Expression { get; }

        public SourceSpan Span => Expression.Span;

        internal Constant(BitsValue value, Expression expression)
        {
            this.Value = value;
            this.Expression = expression;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield return Expression;
        }
    }
}