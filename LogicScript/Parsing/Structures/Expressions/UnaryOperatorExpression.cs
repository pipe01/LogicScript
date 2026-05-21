using System;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class UnaryOperatorExpression(SourceSpan span, Operator op, Expression operand) : Expression(span)
    {
        public Operator Operator { get; set; } = op;
        public Expression Operand { get; set; } = operand;

        public override bool IsConstant => Operand.IsConstant;
        public override int BitSize => Operator switch
        {
            Operator.Not or Operator.Rise or Operator.Fall or Operator.Change => Operand.BitSize,
            Operator.Length => 7,
            Operator.AllOnes => 1,
            _ => throw new ParseException("Unknown unary operator bitsize", Span)
        };

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
