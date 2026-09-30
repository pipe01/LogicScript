using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Visitors;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal class BinaryOperatorExpression(SourceSpan span, BinaryOperator op, Expression left, Expression right) : Expression(span)
    {
        public BinaryOperator Operator { get; set; } = op;
        public Expression Left { get; set; } = left;
        public Expression Right { get; set; } = right;

        public override bool IsConstant => Left.IsConstant && Right.IsConstant;

#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
        public override int BitSize => Operator switch
        {
            BinaryOperator.And or BinaryOperator.Or or BinaryOperator.Xor or BinaryOperator.Subtract or BinaryOperator.Divide => Left.BitSize > Right.BitSize ? Left.BitSize : Right.BitSize,
            BinaryOperator.ShiftLeft => Right.IsConstant ? Left.BitSize + (int)Right.GetValue().Number : Left.BitSize + (1 << Right.BitSize) - 1,
            BinaryOperator.ShiftRight => Right.IsConstant ? Left.BitSize - (int)Right.GetValue().Number : Left.BitSize,
            BinaryOperator.EqualsCompare or BinaryOperator.NotEqualsCompare or BinaryOperator.Greater or BinaryOperator.Lesser or BinaryOperator.AndAlso or BinaryOperator.OrElse => 1,
            BinaryOperator.Add => Left.BitSize > Right.BitSize ? Left.BitSize + 1 : Right.BitSize + 1,
            BinaryOperator.Multiply => Left.BitSize + Right.BitSize,
            BinaryOperator.Power => Left.BitSize * ((1 << Right.BitSize) - 1),
            BinaryOperator.Modulus => Right.BitSize,
        };

        public override Integer ResultType => Operator switch
        {
            BinaryOperator.And or
            BinaryOperator.Or or
            BinaryOperator.Xor or
            BinaryOperator.Add or
            BinaryOperator.Subtract or
            BinaryOperator.Multiply or
            BinaryOperator.Divide or
            BinaryOperator.Modulus => Left.ResultType == Integer.Long || Right.ResultType == Integer.Long ? Integer.Long : Integer.Int,
            BinaryOperator.Power => BitSize.ToIntegerSize(),

            BinaryOperator.ShiftLeft => BitSize.ToIntegerSize(), // Shifting left over the 32-bit boundary produces a 64-bit number
            BinaryOperator.ShiftRight => Left.ResultType,

            // Booleans are treated as int32's on stack
            BinaryOperator.AndAlso or
            BinaryOperator.OrElse or
            BinaryOperator.EqualsCompare or
            BinaryOperator.NotEqualsCompare or
            BinaryOperator.Greater or
            BinaryOperator.Lesser => Integer.Int,
        };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Left;
            yield return Right;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";

        public override BitsValue GetValue(in GetValueContext ctx)
        {
            return Operations.DoOperation(Left.GetValue(ctx), Right.GetValue(ctx), Operator);
        }
    }
}
