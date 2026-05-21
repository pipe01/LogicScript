using System;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Interpreting
{
    internal static class Operations
    {
        public static BitsValue DoOperation(BitsValue left, BitsValue right, Operator op)
        {
            var maxLen = left.Length > right.Length ? left.Length : right.Length;

            return op switch
            {
                Operator.And => new BitsValue(left.Number & right.Number, maxLen),
                Operator.Or => new BitsValue(left.Number | right.Number, maxLen),
                Operator.Xor => new BitsValue(left.Number ^ right.Number, maxLen),
                Operator.ShiftLeft => new BitsValue(left.Number << (int)right.Number, left.Length + (int)right.Number),
                Operator.ShiftRight => new BitsValue(left.Number >> (int)right.Number, left.Length - (int)right.Number),
                Operator.Add => new BitsValue(left.Number + right.Number),
                Operator.Subtract => new BitsValue(left.Number - right.Number),
                Operator.Multiply => new BitsValue(left.Number * right.Number),
                Operator.Divide => new BitsValue(left.Number / right.Number),
                Operator.Power => new BitsValue((ulong)Math.Pow(left.Number, right.Number)),
                Operator.Modulus => new BitsValue(left.Number % right.Number),
                Operator.EqualsCompare => new BitsValue(left.Number == right.Number ? 1ul : 0, 1),
                Operator.NotEqualsCompare => new BitsValue(left.Number != right.Number ? 1ul : 0, 1),
                Operator.Greater => new BitsValue(left.Number > right.Number ? 1ul : 0, 1),
                Operator.Lesser => new BitsValue(left.Number < right.Number ? 1ul : 0, 1),
                _ => throw new InterpreterException("Unknown operator"),
            };
        }

        public static BitsValue Slice(BitsValue value, IndexStart start, int offset, byte length)
        {
            var startIndex = start switch
            {
                IndexStart.Left => offset,
                IndexStart.Right => value.Length - offset - length,
                _ => throw new Exception("Unknown slice start")
            };

            if (startIndex < 0 || startIndex >= value.Length)
                throw new Exception($"Index {startIndex} out of bounds for {value.Length} bits");

            if (length == 1)
                return value[startIndex] ? BitsValue.One : BitsValue.Zero;

            return new BitsValue(value.Bits.AsSpan().Slice(startIndex, length));
        }
    }
}