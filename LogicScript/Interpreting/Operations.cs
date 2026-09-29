using System;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Interpreting
{
    internal static class Operations
    {
        public static BitsValue DoOperation(BitsValue left, BitsValue right, BinaryOperator op)
        {
            var maxLen = left.Length > right.Length ? left.Length : right.Length;

            return op switch
            {
                BinaryOperator.And => new BitsValue(left.Number & right.Number, maxLen),
                BinaryOperator.Or => new BitsValue(left.Number | right.Number, maxLen),
                BinaryOperator.Xor => new BitsValue(left.Number ^ right.Number, maxLen),
                BinaryOperator.ShiftLeft => new BitsValue(left.Number << (int)right.Number, left.Length + (int)right.Number),
                BinaryOperator.ShiftRight => new BitsValue(left.Number >> (int)right.Number, left.Length - (int)right.Number),
                BinaryOperator.Add => new BitsValue(left.Number + right.Number),
                BinaryOperator.Subtract => new BitsValue(left.Number - right.Number),
                BinaryOperator.Multiply => new BitsValue(left.Number * right.Number),
                BinaryOperator.Divide => new BitsValue(left.Number / right.Number),
                BinaryOperator.Power => new BitsValue((ulong)Math.Pow(left.Number, right.Number)),
                BinaryOperator.Modulus => new BitsValue(left.Number % right.Number),
                BinaryOperator.EqualsCompare => new BitsValue(left.Number == right.Number ? 1ul : 0, 1),
                BinaryOperator.NotEqualsCompare => new BitsValue(left.Number != right.Number ? 1ul : 0, 1),
                BinaryOperator.Greater => new BitsValue(left.Number > right.Number ? 1ul : 0, 1),
                BinaryOperator.Lesser => new BitsValue(left.Number < right.Number ? 1ul : 0, 1),
                BinaryOperator.AndAlso => new BitsValue(left.Number != 0 && right.Number != 0 ? 1ul : 0ul, 1),
                BinaryOperator.OrElse => new BitsValue(left.Number != 0 || right.Number != 0 ? 1ul : 0ul, 1),
                _ => throw new InterpreterException("Unknown operator"),
            };
        }

        public static BitsValue Slice(BitsValue value, IndexStart start, int offset, byte length)
        {
            var shift = start switch
            {
                IndexStart.Right => offset,
                IndexStart.Left => value.Length - offset - length,
                _ => throw new Exception("Unknown slice start")
            };

            if (shift < 0 || shift >= value.Length)
                throw new Exception($"Index {shift} out of bounds for {value.Length} bits");

            var mask = (1UL << length) - 1;

            return new BitsValue((value >> shift) & mask, length);
        }
    }
}