using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using System;

namespace LogicScript.Parsing.Visitors
{
    class ExpressionVisitor : LogicScriptBaseVisitor<Expression>
    {
        private readonly Script Script;

        public ExpressionVisitor(Script script)
        {
            this.Script = script;
        }

        public override Expression VisitAtom([NotNull] LogicScriptParser.AtomContext context)
        {
            if (context.DEC_NUMBER() != null)
            {
                var n = ulong.Parse(context.GetText());

                return new NumberLiteralExpression(context.Loc(), new BitsValue(n));
            }
            else if (context.reference() != null)
            {
                var @ref = new ReferenceVisitor(Script).Visit(context.reference());

                if (!@ref.IsReadable)
                    throw new ParseException("An identifier in an expression must be readable", context.reference().Loc());

                return new ReferenceExpression(context.Loc(), @ref);
            }

            throw null;
        }

        public override Expression VisitExprParen([NotNull] LogicScriptParser.ExprParenContext context)
        {
            return Visit(context.expression());
        }

        public override Expression VisitExprXor([NotNull] LogicScriptParser.ExprXorContext context)
        {
            return new BinaryOperatorExpression(context.Loc(), Operator.Xor, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprAndOr([NotNull] LogicScriptParser.ExprAndOrContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.AND => Operator.And,
                LogicScriptParser.OR => Operator.Or,
                _ => throw new Exception("Unknown operator")
            };

            return new BinaryOperatorExpression(context.Loc(), op, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprCompare([NotNull] LogicScriptParser.ExprCompareContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.COMPARE_EQUALS => Operator.EqualsCompare,
                LogicScriptParser.COMPARE_GREATER => Operator.Greater,
                LogicScriptParser.COMPARE_LESSER => Operator.Lesser,
                _ => throw new Exception("Unknown operator")
            };

            return new BinaryOperatorExpression(context.Loc(), op, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprNegate([NotNull] LogicScriptParser.ExprNegateContext context)
        {
            return new UnaryOperatorExpression(context.Loc(), Operator.Not, Visit(context.expression()));
        }
    }
}
