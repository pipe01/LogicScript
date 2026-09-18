using LogicScript.Compiling;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using System;
using System.Collections.Generic;

namespace LogicScript.Interpreting
{
    internal readonly record struct InterpreterContext(IMachine? Machine, IRegisters? Registers, IReadOnlyDictionary<LocalInfo, ulong>? Locals);

    internal class Interpreter
    {
        public static BitsValue Visit(Expression expr, InterpreterContext context = default)
        {
            return expr switch
            {
                NumberLiteralExpression lit => lit.Value,
                BinaryOperatorExpression binOp => Visit(binOp, context),
                ReferenceExpression refExpr => Visit(refExpr, context),
                TernaryOperatorExpression tern => Visit(tern, context),
                UnaryOperatorExpression unary => Visit(unary, context),
                TruncateExpression trunc => Visit(trunc, context),
                SliceExpression slice => Visit(slice, context),
                ReferenceLengthExpression len => len.Value,
                PlaceholderExpression => throw new InterpreterException("Tried to execute placeholder"),
                _ => throw new InterpreterException("Unknown expression", expr.Span.Start),
            };
        }

        private static BitsValue Visit(ReferenceExpression expr, InterpreterContext context)
        {
            switch (expr.Reference)
            {
                case PortReference port:
                    {
                        var vectorIndex = port.VectorIndex == null ? 0 : (int)Visit(port.VectorIndex, context).Number;
                        if (vectorIndex >= port.PortInfo.VectorLength)
                            throw new InterpreterException("Vector index out of range", port.VectorIndex!.Span);

                        return port.PortInfo.Target switch
                        {
                            MachinePorts.Output => throw new InterpreterException("Cannot read from output", expr.Span),
                            MachinePorts.Input => context.Machine == null
                                ? throw new InterpreterException("Can't access inputs on this interpreter runner")
                                : context.Machine.ReadInputs(port.StartIndex + port.BitSize * vectorIndex, port.BitSize),
                            MachinePorts.Register => context.Registers == null
                                ? throw new InterpreterException("Can't access registers on this interpreter runner")
                                : context.Registers.GetRegister(port.StartIndex, vectorIndex),
                            _ => throw new InterpreterException("Unknown reference target", expr.Span),
                        };
                    }

                case LocalReference localReference:
                    if (context.Locals == null)
                        throw new InterpreterException("Can't access locals on interpreter runner");
                    else
                        return context.Locals[localReference.LocalInfo];

                case ConstantReference cnst:
                    return cnst.Constant.Value;
            }

            throw new InterpreterException("Unknown reference type", expr.Span);
        }

        private static BitsValue Visit(BinaryOperatorExpression expr, InterpreterContext context)
        {
            var left = Visit(expr.Left, context);
            var right = Visit(expr.Right, context);

            return Operations.DoOperation(left, right, expr.Operator);
        }

        private static BitsValue Visit(TernaryOperatorExpression expr, InterpreterContext context)
        {
            var cond = Visit(expr.Condition, context);

            if (cond.Number != 0)
                return Visit(expr.IfTrue, context);
            else
                return Visit(expr.IfFalse, context);
        }

        private static BitsValue Visit(UnaryOperatorExpression expr, InterpreterContext context)
        {
            if (expr.Operator == Operator.Length)
                return new BitsValue((ulong)expr.Operand.BitSize, 7);

            var operand = Visit(expr.Operand, context);

            return expr.Operator switch
            {
                Operator.Not => operand.Negated,
                Operator.Rise => throw new NotImplementedException(),
                Operator.Fall => throw new NotImplementedException(),
                Operator.Change => throw new NotImplementedException(),
                Operator.AllOnes => (BitsValue)operand.AreAllBitsSet,
                _ => throw new InterpreterException("Unknown operand", expr.Span),
            };
        }

        private static BitsValue Visit(TruncateExpression expr, InterpreterContext context)
        {
            var operand = Visit(expr.Operand, context);

            return operand.Resize(expr.Size);
        }

        private static BitsValue Visit(SliceExpression expr, InterpreterContext context)
        {
            var operand = Visit(expr.Operand, context);
            var offset = (int)Visit(expr.Offset, context).Number;

            return Operations.Slice(operand, expr.Start, offset, (byte)expr.Length);
        }
    }
}
