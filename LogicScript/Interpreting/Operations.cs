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

            switch (op)
            {
                case Operator.And:
                    return new BitsValue(left.Number & right.Number, maxLen);

                case Operator.Or:
                    return new BitsValue(left.Number | right.Number, maxLen);

                case Operator.Xor:
                    return new BitsValue(left.Number ^ right.Number, maxLen);

                case Operator.ShiftLeft:
                    return new BitsValue(left.Number << (int)right.Number, left.Length + (int)right.Number);

                case Operator.ShiftRight:
                    return new BitsValue(left.Number >> (int)right.Number, left.Length - (int)right.Number);


                case Operator.Add:
                    return new BitsValue(left.Number + right.Number);

                case Operator.Subtract:
                    return new BitsValue(left.Number - right.Number);

                case Operator.Multiply:
                    return new BitsValue(left.Number * right.Number);

                case Operator.Divide:
                    return new BitsValue(left.Number / right.Number);

                case Operator.Power:
                    return new BitsValue((ulong)Math.Pow(left.Number, right.Number));

                case Operator.Modulus:
                    return new BitsValue(left.Number % right.Number);


                case Operator.EqualsCompare:
                    return new BitsValue(left.Number == right.Number ? 1ul : 0, 1);

                case Operator.NotEqualsCompare:
                    return new BitsValue(left.Number != right.Number ? 1ul : 0, 1);

                case Operator.Greater:
                    return new BitsValue(left.Number > right.Number ? 1ul : 0, 1);

                case Operator.Lesser:
                    return new BitsValue(left.Number < right.Number ? 1ul : 0, 1);
            }

            throw new InterpreterException("Unknown operator");
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