using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

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
    }
}