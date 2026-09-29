using System.Collections.Generic;
using LogicScript.Parsing.Visitors;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal class BinaryOperatorExpression(SourceSpan span, BinaryOperator op, Expression left, Expression right) : Expression(span)
    {
        public BinaryOperator Operator { get; set; } = op;
        public Expression Left { get; set; } = left;
        public Expression Right { get; set; } = right;

        public override bool IsConstant => Left.IsConstant && Right.IsConstant;
        public override int BitSize => Operator switch
        {
            BinaryOperator.And or BinaryOperator.Or or BinaryOperator.Xor or BinaryOperator.Subtract or BinaryOperator.Divide => Left.BitSize > Right.BitSize ? Left.BitSize : Right.BitSize,
            BinaryOperator.ShiftLeft => Right.IsConstant ? Left.BitSize + (int)Right.GetConstantValue().Number : Left.BitSize + (1 << Right.BitSize) - 1,
            BinaryOperator.ShiftRight => Right.IsConstant ? Left.BitSize - (int)Right.GetConstantValue().Number : Left.BitSize,
            BinaryOperator.EqualsCompare or BinaryOperator.NotEqualsCompare or BinaryOperator.Greater or BinaryOperator.Lesser or BinaryOperator.AndAlso or BinaryOperator.OrElse => 1,
            BinaryOperator.Add => Left.BitSize > Right.BitSize ? Left.BitSize + 1 : Right.BitSize + 1,
            BinaryOperator.Multiply => Left.BitSize + Right.BitSize,
            BinaryOperator.Power => Left.BitSize * ((1 << Right.BitSize) - 1),
            BinaryOperator.Modulus => Right.BitSize,
            _ => throw new ParseException("Unknown operator bitsize", Span)
        };

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Left;
            yield return Right;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
