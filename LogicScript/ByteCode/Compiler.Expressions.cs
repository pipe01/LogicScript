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
                case NumberLiteralExpression numLit:
                    Visit(numLit);
                    break;

                case ReferenceExpression refExpr:
                    Visit(refExpr);
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
                    Push(expr.Value.Length);
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
                    Push(expr.Value.Length);
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
    }
}