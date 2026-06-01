using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures
{
    public readonly struct Constant : ICodeNode, IPortInfo
    {
        public BitsValue Value { get; }
        internal Expression Expression { get; }

        public SourceSpan Span { get; }

        public int BitSize => Expression.BitSize;

        internal Constant(BitsValue value, Expression expression, SourceSpan nameSpan)
        {
            this.Value = value;
            this.Expression = expression;
            this.Span = nameSpan;
        }

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield return Expression;
        }

        public bool Equals(IPortInfo other) => other is Constant c && c.Value.Equals(Value) && c.Span.Equals(Span);
    }
}