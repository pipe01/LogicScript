using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class UnaryOperatorExpression : Expression
    {
        public Operator Operator { get; set; }
        public Expression Operand { get; set; }

        public override bool IsConstant => Operand.IsConstant;
        public override int BitSize => Operator switch
        {
            Operator.Not or Operator.Rise or Operator.Fall or Operator.Change => Operand.BitSize,
            Operator.Length => 7,
            _ => throw new ParseException("Unknown unary operator bitsize", Span)
        };

        public UnaryOperatorExpression(SourceSpan span, Operator op, Expression operand) : base(span)
        {
            this.Operator = op;
            this.Operand = operand;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
