﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    class ExpressionVisitor : LogicScriptBaseVisitor<Expression>
    {
        private readonly BlockContext Context;
        private readonly int MaxBitSize;

        public ExpressionVisitor(BlockContext context, int maxBitSize = 0)
        {
            this.Context = context;
            this.MaxBitSize = maxBitSize;
        }

        public override Expression Visit([NotNull] IParseTree tree)
        {
            var expr = base.Visit(tree);

            if (MaxBitSize != 0 && expr.BitSize > MaxBitSize)
                throw new ParseException($"Cannot fit a {expr.BitSize} bits long number into {MaxBitSize} bits", tree is ParserRuleContext ctx ? ctx.Loc() : default);

            return expr;
        }

        public override Expression VisitAtom([NotNull] LogicScriptParser.AtomContext context)
        {
            if (context.number() != null)
            {
                var n = new NumberVisitor().Visit(context.number());

                return new NumberLiteralExpression(context.Loc(), n);
            }
            else if (context.reference() != null)
            {
                return Visit(context.reference());
            }

            throw new ParseException("Invalid atom", context.Loc());
        }

        public override Expression VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            if (Context.IsInConstant)
                throw new ParseException("You can only reference constants from other constants", context.Loc());

            var @ref = new ReferenceVisitor(Context).Visit(context);

            return new ReferenceExpression(context.Loc(), @ref);
        }

        public override Expression VisitRefPort([NotNull] LogicScriptParser.RefPortContext context)
        {
            if (Context.Outer.Constants.TryGetValue(context.GetText(), out var val))
                return val;

            if (Context.IsInConstant)
                throw new ParseException("You can only reference constants from other constants", context.Loc());

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

        public override Expression VisitExprPlusMinus([NotNull] LogicScriptParser.ExprPlusMinusContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.PLUS => Operator.Add,
                LogicScriptParser.MINUS => Operator.Subtract,
                _ => throw new ParseException("Unknown operator", context.Loc())
            };

            return new BinaryOperatorExpression(context.Loc(), op, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprMultDiv([NotNull] LogicScriptParser.ExprMultDivContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.MULT => Operator.Multiply,
                LogicScriptParser.DIVIDE => Operator.Divide,
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
                "len" => Operator.Length,
                _ => throw new ParseException($"Unknown function '{context.funcName.Text}'", context.Loc())
            };

            return new UnaryOperatorExpression(context.Loc(), op, operand);
        }

        public override Expression VisitExprTernary([NotNull] LogicScriptParser.ExprTernaryContext context)
        {
            var cond = Visit(context.cond);
            var ifTrue = Visit(context.ifTrue);
            var ifFalse = Visit(context.ifFalse);

            return new TernaryOperatorExpression(context.Loc(), cond, ifTrue, ifFalse);
        }

        public override Expression VisitExprTrunc([NotNull] LogicScriptParser.ExprTruncContext context)
        {
            // Create a new unbounded expression visitor, since we don't care about length
            var operand = new ExpressionVisitor(Context).Visit(context.expression());
            var size = int.Parse(context.DEC_NUMBER().GetText());

            return new TruncateExpression(context.Loc(), operand, size);
        }
    }
}
