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

            throw new InterpreterException("Unknown expression", expr.Location);
        }

        public BitsValue Visit(BinaryOperatorExpression expr)
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

        public BitsValue Visit(ReferenceExpression expr)
        {
            switch (expr.Reference.Target)
            {
                case ReferenceTarget.Output:
                    throw new InterpreterException("Cannot read from output", expr.Location);

                case ReferenceTarget.Input:
                    return Input[Script.Inputs[expr.Reference.Name].Index] ? BitsValue.One : BitsValue.Zero;

                case ReferenceTarget.Register:
                    throw new NotImplementedException();
            }

            throw new InterpreterException("Unknown reference target", expr.Location);
        }
    }
}
