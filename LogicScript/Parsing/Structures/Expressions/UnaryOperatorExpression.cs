using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class UnaryOperatorExpression(SourceSpan span, UnaryOperator op, Expression operand) : Expression(span)
    {
        public UnaryOperator Operator { get; set; } = op;
        public Expression Operand { get; set; } = operand;

        public override bool IsConstant => Operator == UnaryOperator.Length || Operand.IsConstant;
        public override int BitSize => Operator switch
        {
            UnaryOperator.Not or UnaryOperator.Rise or UnaryOperator.Fall or UnaryOperator.Change => Operand.BitSize,
            UnaryOperator.Length => 7,
            UnaryOperator.AllOnes => 1,
            _ => throw new ParseException("Unknown unary operator bitsize", Span)
        };

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
        }

        public override string ToString() => $"{Operator}({Operand})";
    }
}
