using System;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private void Visit(Expression expr)
        {
            switch (expr)
            {
                case BinaryOperatorExpression binOp:
                    Visit(binOp);
                    break;

                case NumberLiteralExpression numLit:
                    Visit(numLit);
                    break;

                case ReferenceExpression refExpr:
                    Visit(refExpr);
                    break;

                case TruncateExpression trunc:
                    Visit(trunc);
                    break;

                case UnaryOperatorExpression unary:
                    Visit(unary);
                    break;

                default:
                    throw new Exception("Unknown expression structure");
            }
        }

        private void Visit(NumberLiteralExpression expr)
        {
            if (expr.Value.Number == 0)
            {
                if (expr.Value.Length == 1)
                {
                    Push(OpCode.Ld_0_1);
                }
                else
                {
                    Push(OpCode.Ld_0);
                    Push((byte)expr.Value.Length);
                }
            }
            else if (expr.Value.Number == 1)
            {
                if (expr.Value.Length == 1)
                {
                    Push(OpCode.Ld_1_1);
                }
                else
                {
                    Push(OpCode.Ld_1);
                    Push((byte)expr.Value.Length);
                }
            }
            else
            {
                if (expr.Value.Length <= 8)
                {
                    Push(OpCode.Ldi_8);
                    Push((byte)(expr.Value.Number & 0xFF));
                }
                else if (expr.Value.Length <= 16)
                {
                    Push(OpCode.Ldi_16);
                    Push((ushort)(expr.Value.Number & 0xFFFF));
                }
                else if (expr.Value.Length <= 32)
                {
                    Push(OpCode.Ldi_32);
                    Push((uint)(expr.Value.Number & 0xFFFFFFFF));
                }
                else
                {
                    Push(OpCode.Ldi_64);
                    Push(expr.Value.Number);
                }

                Push((byte)expr.Value.Length);
            }
        }

        private void Visit(ReferenceExpression expr)
        {
            if (expr.Reference.Port is LocalInfo local)
            {
                if (!LocalsMap.TryGetValue(local.Name, out var index))
                    throw new Exception("Unknown local reference");

                Push(OpCode.Ldloc);
                Push(index);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Visit(TruncateExpression expr)
        {
            Visit(expr.Operand);
            Push(OpCode.Trunc);
            Push((byte)expr.Size);
        }

        private void Visit(BinaryOperatorExpression expr)
        {
            // Push right then left, this way the pop order is left then right
            Visit(expr.Right);
            Visit(expr.Left);

            Push(expr.Operator switch
            {
                Operator.And => OpCode.And,
                Operator.Or => OpCode.Or,
                Operator.Xor => OpCode.Xor,
                Operator.ShiftLeft => OpCode.Shl,
                Operator.ShiftRight => OpCode.Shr,
                Operator.Add => OpCode.Add,
                Operator.Subtract => OpCode.Sub,
                Operator.Multiply => OpCode.Mult,
                Operator.Divide => OpCode.Div,
                Operator.Power => OpCode.Pow,
                Operator.Modulus => OpCode.Mod,
                Operator.EqualsCompare => OpCode.Equals,
                Operator.NotEqualsCompare => OpCode.NotEquals,
                Operator.Greater => OpCode.Greater,
                Operator.Lesser => OpCode.Lesser,
                _ => throw new Exception("Unknown binary operator")
            });
        }

        private void Visit(UnaryOperatorExpression expr)
        {
            Visit(expr.Operand);

            switch (expr.Operator)
            {
                case Operator.Not:
                    Push(OpCode.Not);
                    break;
                case Operator.Rise:
                case Operator.Fall:
                case Operator.Change:
                    throw new NotImplementedException();
                case Operator.Length:
                    Push(OpCode.Length);
                    break;
                case Operator.AllOnes:
                    Push(OpCode.AllOnes);
                    break;
            }
        }
    }
}