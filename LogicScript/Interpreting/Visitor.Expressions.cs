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
            if (expr is NumberLiteralExpression lit)
                return lit.Value;
            if (expr is BinaryOperatorExpression binOp)
                return Visit(binOp);
            if (expr is ReferenceExpression refExpr)
                return Visit(refExpr);
            if (expr is TernaryOperatorExpression tern)
                return Visit(tern);
            if (expr is UnaryOperatorExpression unary)
                return Visit(unary);
            if (expr is TruncateExpression trunc)
                return Visit(trunc);

            throw new InterpreterException("Unknown expression", expr.Location);
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


                case Operator.Add:
                    return new BitsValue(left.Number + right.Number);

                case Operator.Subtract:
                    return new BitsValue(left.Number - right.Number);

                case Operator.Multiply:
                    return new BitsValue(left.Number * right.Number);

                case Operator.Divide:
                    return new BitsValue(left.Number / right.Number);


                case Operator.EqualsCompare:
                    return new BitsValue(left.Number == right.Number && left.Length == right.Length ? 1ul : 0, 1);

                case Operator.Greater:
                    return new BitsValue(left.Number > right.Number && left.Length == right.Length ? 1ul : 0, 1);

                case Operator.Lesser:
                    return new BitsValue(left.Number < right.Number && left.Length == right.Length ? 1ul : 0, 1);


                default:
                    throw new InterpreterException("Unknown operator", expr.Location);
            }
        }

        private BitsValue Visit(ReferenceExpression expr)
        {
            if (expr.Reference is PortReference port)
            {
                switch (port.Target)
                {
                    case ReferenceTarget.Output:
                        throw new InterpreterException("Cannot read from output", expr.Location);

                    case ReferenceTarget.Input:
                        return new BitsValue(Input.Slice(port.StartIndex, port.BitSize));

                    case ReferenceTarget.Register:
                        return Machine.ReadRegister(port.StartIndex);
                }

                throw new InterpreterException("Unknown reference target", expr.Location);
            }
            else if (expr.Reference is LocalReference local)
            {
                return Locals[local.Name];
            }

            throw new InterpreterException("Unknown reference type", expr.Location);
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
            }

            throw new InterpreterException("Unknown operand", expr.Location);
        }

        private BitsValue Visit(TruncateExpression expr)
        {
            return new BitsValue(Visit(expr.Operand), expr.Size);
        }
    }
}
