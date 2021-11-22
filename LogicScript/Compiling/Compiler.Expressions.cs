using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Compiling
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

                case TernaryOperatorExpression tern:
                    Visit(tern);
                    break;

                case NumberLiteralExpression lit:
                    Visit(lit);
                    break;

                case ReferenceExpression refExpr:
                    Visit(refExpr);
                    break;
            }

            throw new InterpreterException("Unknown expression type", expr.Span);
        }

        private void Visit(ReferenceExpression expr)
        {
            if (expr.Reference is PortReference port)
            {
                switch (port.PortInfo.Target)
                {
                    case MachinePorts.Output:
                        throw new InterpreterException("Cannot read from output", expr.Span);

                    case MachinePorts.Input:
                        IL.Ldloca(Input);
                        IL.Ldc_I4(port.StartIndex);
                        IL.Ldc_I4(port.BitSize);
                        IL.Call(typeof(Span<>).MakeGenericType(typeof(bool)).GetMethod("Slice", new[] { typeof(int), typeof(int) }));
                        IL.Newobj(typeof(BitsValue).GetConstructor(new[] { typeof(Span<bool>) }));
                        break;

                    case MachinePorts.Register:
                        LoadMachine();
                        IL.Ldc_I4(port.StartIndex);
                        IL.Call(typeof(IMachine).GetMethod(nameof(IMachine.ReadRegister)));
                        break;

                    default:
                        throw new InterpreterException("Unknown reference target", expr.Span);
                }
            }
            else if (expr.Reference is LocalReference local)
            {
                IL.Ldloc(Locals[local.Name].Local);
            }
            else
            {
                throw new InterpreterException("Unknown reference type", expr.Span);
            }
        }

        private void Visit(BinaryOperatorExpression expr)
        {
            Visit(expr.Left);
            Visit(expr.Right);
            IL.Ldc_I4((int)expr.Operator);
            LoadSpan(expr.Span);
            IL.Call(typeof(Operations).GetMethod(nameof(Operations.DoOperation)));
        }

        private void Visit(TernaryOperatorExpression expr)
        {
            var labelFalse = IL.DefineLabel("tern_false");
            var labelEnd = IL.DefineLabel("tern_end");

            Visit(expr.Condition);

            LoadNumber();
            IL.Brfalse(labelFalse);

            Visit(expr.IfTrue);
            IL.Br(labelEnd);

            IL.MarkLabel(labelFalse);
            IL.Nop();
            Visit(expr.IfFalse);

            IL.MarkLabel(labelEnd);
            IL.Nop();
        }

        private void Visit(NumberLiteralExpression expr)
        {
            LoadBitsValue(expr.Value);
        }
    }
}