using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using System;

namespace LogicScript.Interpreting
{
    partial struct Visitor
    {
        public BitsValue Visit(Expression expr)
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
                _ => throw new InterpreterException("Unknown expression", expr.Span.Start),
            };
        }

        private BitsValue Visit(ReferenceExpression expr)
        {
            AssertNotConstant(expr, "Cannot reference ports from a constant value");

            if (expr.Reference is PortReference port)
            {
                switch (port.PortInfo.Target)
                {
                    case MachinePorts.Output:
                        throw new InterpreterException("Cannot read from output", expr.Span);

                    case MachinePorts.Input:
                        return new BitsValue(Input.Slice(port.StartIndex, port.BitSize));

                    case MachinePorts.Register:
                        return Machine.ReadRegister(port.StartIndex);
                }

                throw new InterpreterException("Unknown reference target", expr.Span);
            }
            else if (expr.Reference is LocalReference local)
            {
                return Locals[local.LocalInfo.Name];
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
            var operand = Visit(expr.Operand);

            switch (expr.Operator)
            {
                case Operator.Not:
                    return operand.Negated;
                case Operator.Rise:
                    throw new NotImplementedException();
                case Operator.Fall:
                    throw new NotImplementedException();
                case Operator.Change:
                    throw new NotImplementedException();
                case Operator.Length:
                    return new BitsValue((ulong)operand.Length, 7);
                case Operator.AllOnes:
                    return operand.AreAllBitsSet;
            }

            throw new InterpreterException("Unknown operand", expr.Span);
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
