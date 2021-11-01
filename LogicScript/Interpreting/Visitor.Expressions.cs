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
            if (expr.Reference is PortReference port)
            {
                switch (port.Target)
                {
                    case ReferenceTarget.Output:
                        throw new InterpreterException("Cannot read from output", expr.Span);

                    case ReferenceTarget.Input:
                        return new BitsValue(Input.Slice(port.StartIndex, port.BitSize));

                    case ReferenceTarget.Register:
                        return Machine.ReadRegister(port.StartIndex);
                }

                throw new InterpreterException("Unknown reference target", expr.Span);
            }
            else if (expr.Reference is LocalReference local)
            {
                return Locals[local.Name];
            }

            throw new InterpreterException("Unknown reference type", expr.Span);
        }

        private BitsValue Visit(BinaryOperatorExpression expr)
        {
            var left = Visit(expr.Left);
            var right = Visit(expr.Right);
            var maxLen = left.Length > right.Length ? left.Length : right.Length;

            switch (expr.Operator)
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


                case Operator.EqualsCompare:
                    return new BitsValue(left.Number == right.Number ? 1ul : 0, 1);

                case Operator.Greater:
                    return new BitsValue(left.Number > right.Number ? 1ul : 0, 1);

                case Operator.Lesser:
                    return new BitsValue(left.Number < right.Number ? 1ul : 0, 1);


                default:
                    throw new InterpreterException("Unknown operator", expr.Span);
            }
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
            var startIndex = expr.Start switch
            {
                IndexStart.Left => expr.Offset,
                IndexStart.Right => operand.Length - expr.Offset - 1,
                _ => throw new InterpreterException("Unknown slice start", expr.Span)
            };

            if (startIndex < 0 || startIndex >= operand.Length)
                throw new InterpreterException($"Index {startIndex} out of bounds for {operand.Length} bits", expr.Span);

            if (expr.Length == 1)
                return operand[startIndex] ? BitsValue.One : BitsValue.Zero;

            return new BitsValue(operand.Bits.AsSpan().Slice(startIndex, expr.Length));
        }
    }
}
