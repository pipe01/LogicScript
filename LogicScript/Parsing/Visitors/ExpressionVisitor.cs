using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Parsing.Visitors
{
    class ExpressionVisitor : LogicScriptBaseVisitor<Expression>
    {
        public override Expression VisitAtom([NotNull] LogicScriptParser.AtomContext context)
        {
            if (context.DEC_NUMBER() != null)
            {
                var n = ulong.Parse(context.GetText());

                return new NumberLiteralExpression(new BitsValue(n), context.StartLocation());
            }
            else if (context.IDENT() != null)
            {
                return new VariableAccessExpression(context.GetText(), context.StartLocation());
            }

            throw null;
        }

        public override Expression VisitExprParen([NotNull] LogicScriptParser.ExprParenContext context)
        {
            return Visit(context.expression());
        }

        public override Expression VisitExprXor([NotNull] LogicScriptParser.ExprXorContext context)
        {
            return new OperatorExpression(Operator.Xor, Visit(context.expression(0)), Visit(context.expression(1)), context.StartLocation());
        }

        public override Expression VisitExprAndOr([NotNull] LogicScriptParser.ExprAndOrContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.AND => Operator.And,
                LogicScriptParser.OR => Operator.Or,
                _ => throw new Exception("Unknown operator")
            };

            return new OperatorExpression(op, Visit(context.expression(0)), Visit(context.expression(1)), context.StartLocation());
        }

        public override Expression VisitExprCompare([NotNull] LogicScriptParser.ExprCompareContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.COMPARE_EQUALS => Operator.Equals,
                LogicScriptParser.COMPARE_GREATER => Operator.Greater,
                LogicScriptParser.COMPARE_LESSER => Operator.Lesser,
                _ => throw new Exception("Unknown operator")
            };

            return new OperatorExpression(op, Visit(context.expression(0)), Visit(context.expression(1)), context.StartLocation());
        }

        public override Expression VisitExprNegate([NotNull] LogicScriptParser.ExprNegateContext context)
        {
            return new UnaryOperatorExpression(Operator.Not, Visit(context.expression()), context.StartLocation());
        }
    }
}
