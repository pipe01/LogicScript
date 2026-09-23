using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    class ExpressionVisitor(BlockContext context, int? maxBitSize = null) : LogicScriptParserBaseVisitor<Expression>
    {
        private readonly BlockContext Context = context;
        private readonly int? MaxBitSize = maxBitSize;

        public Expression VisitOrPlaceholder(IParseTree tree, SourceSpan defaultSpan)
        {
            if (tree == null)
                return new PlaceholderExpression(defaultSpan, MaxBitSize ?? 0);
            return Visit(tree);
        }

        public override Expression Visit([NotNull] IParseTree tree)
        {
            var expr = base.Visit(tree);

            if (expr == null)
            {
                if (tree is ParserRuleContext ctx)
                    return new PlaceholderExpression(ctx.Span(), MaxBitSize ?? 0);
                else
                    throw new ParseCanceledException();
            }

            if (MaxBitSize != null && expr.BitSize > MaxBitSize)
            {
                if (Context.Script.TruncatePragmaMode is TruncatePragmaMode.Implicit or TruncatePragmaMode.Warn)
                {
                    if (Context.Script.TruncatePragmaMode == TruncatePragmaMode.Warn)
                        Context.Errors.AddExpressionTooLarge(expr.BitSize, MaxBitSize.Value, expr, Severity.Warning);

                    return new TruncateExpression(expr.Span, expr, MaxBitSize.Value, null);
                }
                else
                {
                    Context.Errors.AddExpressionTooLarge(expr.BitSize, MaxBitSize.Value, expr, Severity.Error);
                }
            }

            return expr;
        }

        public override Expression VisitAtom([NotNull] LogicScriptParser.AtomContext context)
        {
            if (context.number() != null)
            {
                var n = new NumberVisitor().Visit(context.number());

                return new NumberLiteralExpression(context.Span(), new BitsValue(n, Math.Max(MaxBitSize ?? 0, n.Length)));
            }
            else if (context.reference() != null)
            {
                return Visit(context.reference());
            }

            throw new ParseException("Invalid atom", context.Span());
        }

        public override Expression VisitRefLocal([NotNull] LogicScriptParser.RefLocalContext context)
        {
            if (Context.IsInConstant)
                Context.Errors.AddConstantReferenceRequired(context.Span());

            var @ref = new ReferenceVisitor(Context, MaxBitSize ?? 0).Visit(context);

            return new ReferenceExpression(context.Span(), @ref);
        }

        public override Expression VisitRefPort([NotNull] LogicScriptParser.RefPortContext context)
        {
            if (Context.Script.Script.Constants.TryGetValue(context.GetText(), out var @const))
                return new ReferenceExpression(context.Span(), new ConstantReference(context.Span(), @const));

            if (Context.IsInConstant)
                Context.Errors.AddConstantReferenceRequired(context.Span());

            var @ref = new ReferenceVisitor(Context, MaxBitSize ?? 0).Visit(context);

            if (!@ref.IsReadable)
                Context.Errors.AddExpressionReferenceNotReadable(context.Span());

            return new ReferenceExpression(context.Span(), @ref);
        }

        public override Expression VisitRefIndex([NotNull] LogicScriptParser.RefIndexContext context)
        {
            if (Context.IsInConstant)
                Context.Errors.AddConstantReferenceRequired(context.Span());

            var @ref = new ReferenceVisitor(Context, MaxBitSize ?? 0).Visit(context);

            return new ReferenceExpression(context.Span(), @ref);
        }

        public override Expression VisitExprParen([NotNull] LogicScriptParser.ExprParenContext context)
        {
            return Visit(context.expression());
        }

        public override Expression VisitExprSlice([NotNull] LogicScriptParser.ExprSliceContext context)
        {
            // Create a new unbounded expression visitor, since we only care about the slice length, not the operand's
            var operand = new ExpressionVisitor(Context).Visit(context.expression());
            var start = context.slice_indexer().lr?.Text switch
            {
                ">" => IndexStart.Right,
                "<" or null => IndexStart.Left,
                _ => throw new ParseException("Unknown index start position", context.slice_indexer().Span())
            };

            Expression offset;

            if (context.slice_indexer().offset == null)
            {
                Context.Errors.AddIndexerOffsetMissing(context.slice_indexer().Span());
                offset = new NumberLiteralExpression(context.slice_indexer().Span(), BitsValue.Zero);
            }
            else
            {
                offset = new ExpressionVisitor(Context).Visit(context.slice_indexer().offset);
            }

            var length = context.slice_indexer().len == null ? 1 : (int)context.slice_indexer().len.GetConstantValue(Context.Script);
            var sliceExpr = new SliceExpression(context.Span(), operand, start, offset, length);

            if (length == 0)
                Context.Errors.AddSliceLengthZero(context.slice_indexer().len.Span());

            if (offset.IsConstant)
            {
                //TODO Figure out the logic for left-indexed slices
                var offsetValue = (int)offset.GetConstantValue().Number;

                if (offsetValue >= operand.BitSize)
                    Context.Errors.AddSliceOffsetOutOfBounds(context.slice_indexer().offset.Span());

                if (offsetValue + length > operand.BitSize)
                    Context.Errors.AddSliceOutOfBounds(context.slice_indexer().Span());
            }

            if (MaxBitSize != 0 && length > MaxBitSize)
                Context.Errors.AddExpressionTooLarge(length, MaxBitSize.Value, sliceExpr, Severity.Error);

            return sliceExpr;
        }

        public override Expression VisitExprBinOp([NotNull] LogicScriptParser.ExprBinOpContext context)
        {
            var op = context.op.Type switch
            {
                LogicScriptParser.OR => Operator.Or,
                LogicScriptParser.AND => Operator.And,
                LogicScriptParser.XOR => Operator.Xor,
                LogicScriptParser.POW => Operator.Power,
                LogicScriptParser.PLUS => Operator.Add,
                LogicScriptParser.MINUS => Operator.Subtract,
                LogicScriptParser.MULT => Operator.Multiply,
                LogicScriptParser.DIVIDE => Operator.Divide,
                LogicScriptParser.MOD => Operator.Modulus,
                LogicScriptParser.LSHIFT => Operator.ShiftLeft,
                LogicScriptParser.RSHIFT => Operator.ShiftRight,
                LogicScriptParser.COMPARE_EQUALS => Operator.EqualsCompare,
                LogicScriptParser.COMPARE_NOTEQUALS => Operator.NotEqualsCompare,
                LogicScriptParser.COMPARE_GREATER => Operator.Greater,
                LogicScriptParser.COMPARE_LESSER => Operator.Lesser,
                _ => throw new ParseException("Unknown operator", context.Span())
            };

            return new BinaryOperatorExpression(context.Span(), op, Visit(context.expression(0)), Visit(context.expression(1)));
        }

        public override Expression VisitExprNegate([NotNull] LogicScriptParser.ExprNegateContext context)
        {
            return new UnaryOperatorExpression(context.Span(), Operator.Not, Visit(context.expression()));
        }

        private static readonly Dictionary<string, Operator> UnaryFunctions = new()
        {
            ["rise"] = Operator.Rise,
            ["fall"] = Operator.Fall,
            ["change"] = Operator.Change,
            ["allOnes"] = Operator.AllOnes,
        };

        public override Expression VisitExprCall([NotNull] LogicScriptParser.ExprCallContext context)
        {
            var name = context.funcName.Text;

            if (UnaryFunctions.TryGetValue(name, out var op))
            {
                if (context.arg_list() == null)
                {
                    Context.Errors.AddOperandRequired(name, context.Span());
                    return new PlaceholderExpression(context.Span());
                }

                if (context.arg_list().arg_list() != null)
                    Context.Errors.AddSingleParameterRequired(name, context.Span());

                if (op is Operator.Rise or Operator.Fall or Operator.Change)
                    Context.Errors.AddOperatorNotImplemented(op.ToString().ToLower(), context.funcName.Span());

                var value = Visit(context.arg_list().value);

                return new UnaryOperatorExpression(context.Span(), op, value);
            }

            if (!Context.Script.Script.Functions.TryGetValue(name, out var function))
            {
                Context.Errors.AddFunctionNotFound(name, context.funcName.Span());
                return new PlaceholderExpression(context.Span());
            }

            var args = Flatten(context.arg_list())
                .Select((c, i) => new ExpressionVisitor(Context, i < function.Parameters.Length ? function.Parameters[i].BitSize : null).Visit(c))
                .ToArray();

            if (args.Length != function.Parameters.Length)
                Context.Errors.AddFunctionArgumentCountMismatch(name, function.Parameters.Length, args.Length, context.Span());

            return new FunctionCallExpression(context.Span(), function, args);
        }

        public override Expression VisitExprLength([NotNull] LogicScriptParser.ExprLengthContext context)
        {
            // When using len(x), the result is always a constant even if x isn't.
            // Thus, when visiting the operand we allow references to non-constant values as they won't ever actually be evaluated.

            if (context.reference() != null)
            {
                var name = context.reference().GetText();
                if (Context.Script.Script.Constants.TryGetValue(name, out var @const))
                    return new UnaryOperatorExpression(context.Span(), Operator.Length, @const.Expression);

                var reference = new ReferenceVisitor(Context, MaxBitSize ?? 0, true).Visit(context.reference());
                return new ReferenceLengthExpression(context.Span(), reference);
            }

            if (context.expression() != null)
            {
                var value = new ExpressionVisitor(new BlockContext(Context.Script, Context.Outer)).Visit(context.expression());
                return new UnaryOperatorExpression(context.Span(), Operator.Length, value);
            }

            // For partial parsing
            return new UnaryOperatorExpression(context.Span(), Operator.Length, new PlaceholderExpression(context.Span()));
        }

        public override Expression VisitExprTernary([NotNull] LogicScriptParser.ExprTernaryContext context)
        {
            var cond = Visit(context.cond);
            var ifTrue = Visit(context.ifTrue);
            var ifFalse = Visit(context.ifFalse);

            return new TernaryOperatorExpression(context.Span(), cond, ifTrue, ifFalse);
        }

        public override Expression VisitExprTrunc([NotNull] LogicScriptParser.ExprTruncContext context)
        {
            // Create a new unbounded expression visitor since we don't care about length
            var operand = new ExpressionVisitor(Context).Visit(context.expression(0));
            var size = Context.Script.ParseBitSize(context.size, out var sizeExpr);

            return new TruncateExpression(context.Span(), operand, size, sizeExpr);
        }

        private IEnumerable<LogicScriptParser.ExpressionContext> Flatten(LogicScriptParser.Arg_listContext context)
        {
            if (context == null)
                yield break;

            yield return context.expression();

            foreach (var arg in Flatten(context.arg_list()))
                yield return arg;
        }
    }
}
