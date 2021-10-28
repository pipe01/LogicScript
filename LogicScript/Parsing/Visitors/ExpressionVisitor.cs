using Antlr4.Runtime.Misc;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    class ExpressionVisitor : LogicScriptBaseVisitor<Expression>
    {
        private readonly VisitContext Context;

        public ExpressionVisitor(VisitContext context)
        {
            this.Context = context;
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
                return Visit(context.reference());
            }

            throw new ParseException("Invalid atom", context.Loc());
        }

        public override Expression VisitReference([NotNull] LogicScriptParser.ReferenceContext context)
        {
            if (Context.Constants.TryGetValue(context.GetText(), out var val))
                return val;

            var @ref = new ReferenceVisitor(Context).Visit(context);

            if (!@ref.IsReadable)
                throw new ParseException("An identifier in an expression must be readable", context.Loc());

            return new ReferenceExpression(context.Loc(), @ref);
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
                _ => throw new ParseException("Unknown operator", context.Loc())
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
                _ => throw new ParseException("Unknown operator", context.Loc())
            };

            return new BinaryOperatorExpression(context.Loc(), op, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprNegate([NotNull] LogicScriptParser.ExprNegateContext context)
        {
            return new UnaryOperatorExpression(context.Loc(), Operator.Not, Visit(context.expression()));
        }

        public override Expression VisitExprCall([NotNull] LogicScriptParser.ExprCallContext context)
        {
            var operand = Visit(context.expression());

            var op = context.funcName.Text switch
            {
                "rise" => Operator.Rise,
                "fall" => Operator.Fall,
                "change" => Operator.Change,
                _ => throw new ParseException($"Unknown function '{context.funcName.Text}'", context.Loc())
            };

            return new UnaryOperatorExpression(context.Loc(), op, operand);
        }
    }
}
