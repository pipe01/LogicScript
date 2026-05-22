using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Utils;
using System.Linq;

namespace LogicScript.Parsing.Visitors
{
    class StatementVisitor(ScriptContext context, BlockContext? blockContext = null) : LogicScriptBaseVisitor<Statement>
    {
        private readonly ScriptContext Context = context;
        private readonly BlockContext BlockContext = blockContext ?? new BlockContext(context);

        public override Statement VisitBlock([NotNull] LogicScriptParser.BlockContext context)
        {
            return VisitBlock(context, BlockContext.LoopID);
        }

        public Statement VisitBlock([NotNull] LogicScriptParser.BlockContext context, NodeID? loopID)
        {
            var blockContext = new BlockContext(Context, BlockContext, BlockContext.IsInConstant, loopID);
            var visitor = new StatementVisitor(Context, blockContext);

            var stmts = context.stmt().Select(visitor.Visit).ToArray();

            return new BlockStatement(context.Span(), stmts, blockContext.Locals);
        }

        public override Statement VisitAssignRegular([NotNull] LogicScriptParser.AssignRegularContext context)
        {
            var @ref = new ReferenceVisitor(BlockContext).Visit(context.reference());

            if (!@ref.IsWritable)
                Context.Errors.AddError("The left hand side of an assignment must be writable", context.reference().Span());

            var value = new ExpressionVisitor(BlockContext, @ref.BitSize).Visit(context.expression());

            return new AssignStatement(context.Span(), @ref, value);
        }

        public override Statement VisitAssignTruncate([NotNull] LogicScriptParser.AssignTruncateContext context)
        {
            var @ref = new ReferenceVisitor(BlockContext).Visit(context.reference());

            if (!@ref.IsWritable)
                Context.Errors.AddError("The left hand side of an assignment must be writable", context.reference().Span());

            var value = new ExpressionVisitor(BlockContext).Visit(context.expression());
            var truncated = new TruncateExpression(context.Span(), value, @ref.BitSize);

            return new AssignStatement(context.Span(), @ref, truncated);
        }

        public override Statement VisitStmt_if([NotNull] LogicScriptParser.Stmt_ifContext context)
        {
            return VisitIfBody(context.if_body());
        }

        private IfStatement VisitIfBody(LogicScriptParser.If_bodyContext context)
        {
            var cond = new ExpressionVisitor(BlockContext).Visit(context.expression());
            var body = Visit(context.block());
            Statement? @else = null;

            if (context.stmt_else() != null)
            {
                @else = Visit(context.stmt_else().block());
            }
            else if (context.stmt_elseif() != null)
            {
                @else = VisitIfBody(context.stmt_elseif().if_body());
            }

            return new IfStatement(context.Span(), cond, body, @else);
        }

        public override Statement VisitStmt_for([NotNull] LogicScriptParser.Stmt_forContext context)
        {
            var varName = context.VARIABLE().GetText();
            var from = context.from == null ? null : new ExpressionVisitor(BlockContext).Visit(context.from);
            var to = new ExpressionVisitor(BlockContext).Visit(context.to);

            // TODO: Scope the local inside the loop, not outside
            var local = BlockContext.TryGetLocal(varName, out var l)
                ? l
                : BlockContext.AddLocal(varName, to.BitSize, new SourceSpan(context.VARIABLE().Symbol));

            if (local.BitSize < to.BitSize) // TODO: The loop doesn't actually reach the value in 'to' as it's exclusive, so the required bit size may be one smaller than what's currently computed
                Context.Errors.AddError($"The loop variable '{varName}' is too small to hold the loop limit", new SourceSpan(context.VARIABLE().Symbol));

            var id = NodeID.Next();
            var body = VisitBlock(context.block(), id);

            return new ForStatement(id, context.Span(), local, from, to, body);
        }

        public override Statement VisitStmt_while([NotNull] LogicScriptParser.Stmt_whileContext context)
        {
            var cond = new ExpressionVisitor(BlockContext).Visit(context.expression());

            var id = NodeID.Next();
            var body = VisitBlock(context.block(), id);

            return new WhileStatement(id, context.Span(), cond, body);
        }

        public override Statement VisitStmt_vardecl([NotNull] LogicScriptParser.Stmt_vardeclContext context)
        {
            var name = context.VARIABLE().GetText();

            Expression? value = null;

            // If the variable has a bit size marker, we will use that size. Otherwise, we will later infer it from the value
            int size = context.size == null ? 0 : context.size.GetConstantValue(BlockContext.Script);

            if (context.expression() != null)
            {
                value = new ExpressionVisitor(BlockContext, size).Visit(context.expression());

                if (size == 0)
                    size = value.BitSize;
            }
            else if (size == 0)
            {
                BlockContext.Errors.AddError("You must specify a local's size or initialize it", context.Span(), true);
            }

            if (BlockContext.TryGetLocal(name, out var existingLocal, checkOuter: false))
            {
                BlockContext.Errors.AddError($"Identifier '{name}' already exists", new SourceSpan(context.VARIABLE().Symbol));
                return new DeclareLocalStatement(context.Span(), existingLocal, value);
            }

            var localInfo = BlockContext.AddLocal(name, size, new SourceSpan(context.VARIABLE().Symbol));

            return new DeclareLocalStatement(context.Span(), localInfo, value);
        }

        public override Statement VisitTask_print([NotNull] LogicScriptParser.Task_printContext context)
        {
            if (context.expression() != null)
            {
                var value = new ExpressionVisitor(BlockContext).Visit(context.expression());

                return new ShowTaskStatement(context.Span(), value);
            }
            else if (context.TEXT() != null)
            {
                var text = context.TEXT().GetText().Trim('"');

                var formatString = PrintStringFormat.Parse(text, name =>
                {
                    if (BlockContext.TryGetLocal(name, out var local))
                        return local;

                    throw new ParseException($"Unknown local '{name}' in format string", context.Span());
                });

                return new PrintTaskStatement(context.Span(), formatString);
            }

            throw new ParseException("Invalid print value", context.Span());
        }

        public override Statement VisitTask_update([NotNull] LogicScriptParser.Task_updateContext context)
        {
            return new UpdateTaskStatement(context.Span());
        }

        public override Statement VisitStmt_break([NotNull] LogicScriptParser.Stmt_breakContext context)
        {
            if (BlockContext.LoopID == null)
                Context.Errors.AddError("Break statements can only be used inside loops", context.Span());

            return new BreakStatement(context.Span(), BlockContext.LoopID.GetValueOrDefault());
        }
    }
}
