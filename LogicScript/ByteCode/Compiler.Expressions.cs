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

                case SliceExpression slice:
                    Visit(slice);
                    break;

                case TernaryOperatorExpression ternary:
                    Visit(ternary);
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
                    Push(OpCodes.Ld_0_1);
                }
                else
                {
                    Push(OpCodes.Ld_0);
                    Push((byte)expr.Value.Length);
                }
            }
            else if (expr.Value.Number == 1)
            {
                if (expr.Value.Length == 1)
                {
                    Push(OpCodes.Ld_1_1);
                }
                else
                {
                    Push(OpCodes.Ld_1);
                    Push((byte)expr.Value.Length);
                }
            }
            else
            {
                if (expr.Value.Length <= 8)
                {
                    Push(OpCodes.Ldi_8);
                    Push((byte)(expr.Value.Number & 0xFF));
                }
                else if (expr.Value.Length <= 16)
                {
                    Push(OpCodes.Ldi_16);
                    Push((ushort)(expr.Value.Number & 0xFFFF));
                }
                else if (expr.Value.Length <= 32)
                {
                    Push(OpCodes.Ldi_32);
                    Push((uint)(expr.Value.Number & 0xFFFFFFFF));
                }
                else
                {
                    Push(OpCodes.Ldi_64);
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

                Push(OpCodes.Ldloc);
                Push(index);
            }
            else if (expr.Reference.Port is PortInfo port)
            {
                switch (port.Target)
                {
                    case MachinePorts.Input:
                        Push(OpCodes.LoadPortInput);
                        Push((byte)port.StartIndex);
                        Push((byte)port.BitSize);
                        break;

                    case MachinePorts.Register:
                        Push(OpCodes.LoadPortRegister);
                        Push((byte)port.StartIndex);
                        break;
                }
            }
        }

        private void Visit(TruncateExpression expr)
        {
            Visit(expr.Operand);
            Push(OpCodes.Trunc);
            Push((byte)expr.Size);
        }

        private void Visit(BinaryOperatorExpression expr)
        {
            // Push right then left, this way the pop order is left then right
            Visit(expr.Right);
            Visit(expr.Left);

            Push(expr.Operator switch
            {
                Operator.And => OpCodes.And,
                Operator.Or => OpCodes.Or,
                Operator.Xor => OpCodes.Xor,
                Operator.ShiftLeft => OpCodes.Shl,
                Operator.ShiftRight => OpCodes.Shr,
                Operator.Add => OpCodes.Add,
                Operator.Subtract => OpCodes.Sub,
                Operator.Multiply => OpCodes.Mult,
                Operator.Divide => OpCodes.Div,
                Operator.Power => OpCodes.Pow,
                Operator.Modulus => OpCodes.Mod,
                Operator.EqualsCompare => OpCodes.Equals,
                Operator.NotEqualsCompare => OpCodes.NotEquals,
                Operator.Greater => OpCodes.Greater,
                Operator.Lesser => OpCodes.Lesser,
                _ => throw new Exception("Unknown binary operator")
            });
        }

        private void Visit(UnaryOperatorExpression expr)
        {
            Visit(expr.Operand);

            switch (expr.Operator)
            {
                case Operator.Not:
                    Push(OpCodes.Not);
                    break;
                case Operator.Rise:
                case Operator.Fall:
                case Operator.Change:
                    throw new NotImplementedException();
                case Operator.Length:
                    Push(OpCodes.Length);
                    break;
                case Operator.AllOnes:
                    Push(OpCodes.AllOnes);
                    break;
            }
        }

        private void Visit(TernaryOperatorExpression expr)
        {
            var endLabel = NewLabel();
            var falseLabel = NewLabel();

            Visit(expr.Condition);
            Jump(OpCodes.Brz, falseLabel);

            Visit(expr.IfTrue);
            Jump(OpCodes.Jmp, endLabel);

            Push(OpCodes.Nop);
            MarkLabel(falseLabel);

            Visit(expr.IfFalse);

            Push(OpCodes.Nop);
            MarkLabel(endLabel);
        }

        private void Visit(SliceExpression expr)
        {
            Visit(expr.Offset);
            Visit(expr.Operand);

            Push(expr.Start switch {
                IndexStart.Left => OpCodes.SliceLeft,
                IndexStart.Right => OpCodes.SliceRight,
                _ => throw new Exception("Invalid slice index start")
            });
            Push((byte)expr.Length);
        }
    }
}