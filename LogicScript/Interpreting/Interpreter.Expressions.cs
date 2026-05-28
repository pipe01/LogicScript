using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using System;

namespace LogicScript.Interpreting
{
    partial class Interpreter
    {
        private BitsValue Visit(Expression expr)
        {
            return expr switch
            {
                NumberLiteralExpression lit => lit.Value,
                BinaryOperatorExpression binOp => Visit(binOp),
                ReferenceExpression refExpr => Visit(refExpr),
                TernaryOperatorExpression tern => Visit(tern),
                UnaryOperatorExpression unary => Visit(unary),
                TruncateExpression trunc => Visit(trunc),
                SliceExpression slice => Visit(slice),
                ReferenceLengthExpression len => len.Value,
                PlaceholderExpression => throw new InterpreterException("Tried to execute placeholder"),
                _ => throw new InterpreterException("Unknown expression", expr.Span.Start),
            };
        }

        private BitsValue Visit(ReferenceExpression expr)
        {
            if (expr.Reference is PortReference port)
            {
                var vectorIndex = port.VectorIndex == null ? 0 : (int)Visit(port.VectorIndex).Number;
                if (vectorIndex >= port.PortInfo.VectorLength)
                    throw new InterpreterException("Vector index out of range", port.VectorIndex!.Span);

                return port.PortInfo.Target switch
                {
                    MachinePorts.Output => throw new InterpreterException("Cannot read from output", expr.Span),
                    MachinePorts.Input => new BitsValue(Machine!.ReadInputs().Slice(port.StartIndex + port.BitSize * vectorIndex, port.BitSize)),
                    MachinePorts.Register => new BitsValue(Machine!.ReadRegister(port.StartIndex + vectorIndex), expr.BitSize),
                    _ => throw new InterpreterException("Unknown reference target", expr.Span),
                };
            }
            else if (expr.Reference is LocalReference local)
            {
                return Locals[local.LocalInfo];
            }

            throw new InterpreterException("Unknown reference type", expr.Span);
        }

        private BitsValue Visit(BinaryOperatorExpression expr)
        {
            var left = Visit(expr.Left);
            var right = Visit(expr.Right);

            return Operations.DoOperation(left, right, expr.Operator);
        }

        private BitsValue Visit(TernaryOperatorExpression expr)
        {
            var cond = Visit(expr.Condition);

            if (cond.Number != 0)
                return Visit(expr.IfTrue);
            else
                return Visit(expr.IfFalse);
        }

        private BitsValue Visit(UnaryOperatorExpression expr)
        {
            if (expr.Operator == Operator.Length)
            {
                if (expr.Operand.IsConstant)
                    return GetConstantValue(expr.Operand).Length;

                return new BitsValue((ulong)expr.Operand.BitSize, 7);
            }

            var operand = Visit(expr.Operand);

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

        private BitsValue Visit(TruncateExpression expr)
        {
            var operand = Visit(expr.Operand);

            return operand.Resize(expr.Size);
        }

        private BitsValue Visit(SliceExpression expr)
        {
            var operand = Visit(expr.Operand);
            var offset = (int)Visit(expr.Offset).Number;

            return Operations.Slice(operand, expr.Start, offset, (byte)expr.Length);
        }
    }
}
