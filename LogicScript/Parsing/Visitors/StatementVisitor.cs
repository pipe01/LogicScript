using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System.Linq;
using System.Net.Mime;

namespace LogicScript.Parsing.Visitors
{
    class StatementVisitor : LogicScriptBaseVisitor<Statement>
    {
        private readonly BlockContext Context;

        public StatementVisitor(BlockContext context)
        {
            this.Context = context;
        }

        public override Statement VisitBlock([NotNull] LogicScriptParser.BlockContext context)
        {
            return new BlockStatement(context.Span(), context.stmt().Select(Visit).ToArray());
        }

        public override Statement VisitAssignRegular([NotNull] LogicScriptParser.AssignRegularContext context)
        {
            var @ref = new ReferenceVisitor(Context).Visit(context.reference());

            if (!@ref.IsWritable)
                Context.Errors.AddError("The left hand side of an assignment must be writable", context.reference().Span());

            var value = new ExpressionVisitor(Context, @ref.BitSize).Visit(context.expression());

            return new AssignStatement(context.Span(), @ref, value, null);
        }

        public override Statement VisitAssignTruncate([NotNull] LogicScriptParser.AssignTruncateContext context)
        {
            var @ref = new ReferenceVisitor(Context).Visit(context.reference());

            if (!@ref.IsWritable)
                Context.Errors.AddError("The left hand side of an assignment must be writable", context.reference().Span());

            var value = new ExpressionVisitor(Context).Visit(context.expression());

            return new AssignStatement(context.Span(), @ref, value, @ref.BitSize);
        }

        public override Statement VisitStmt_if([NotNull] LogicScriptParser.Stmt_ifContext context)
        {
            return VisitIfBody(context.if_body());
        }

        private IfStatement VisitIfBody(LogicScriptParser.If_bodyContext context)
        {
            var cond = new ExpressionVisitor(Context).Visit(context.expression());
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
            var varName = context.VARIABLE().GetText().TrimStart('$');
            var from = context.from == null ? null : new ExpressionVisitor(Context).Visit(context.from);
            var to = new ExpressionVisitor(Context).Visit(context.to);

            if (!Context.Locals.ContainsKey(varName))
                Context.Locals.Add(varName, new LocalInfo(to.BitSize, varName, new SourceSpan(context.VARIABLE().Symbol)));

            var body = Visit(context.block());

            return new ForStatement(context.Span(), varName, from, to, body);
        }

        public override Statement VisitStmt_while([NotNull] LogicScriptParser.Stmt_whileContext context)
        {
            var cond = new ExpressionVisitor(Context).Visit(context.expression());
            var body = Visit(context.block());

            return new WhileStatement(context.Span(), cond, body);
        }

        public override Statement VisitStmt_vardecl([NotNull] LogicScriptParser.Stmt_vardeclContext context)
        {
            var name = context.VARIABLE().GetText().TrimStart('$');

            if (Context.DoesIdentifierExist(name))
                Context.Errors.AddError($"Identifier '{name}' already exists", new SourceSpan(context.VARIABLE().Symbol), true);

            Expression? value = null;

            // If the variable has a bit size marker, we will use that size. Otherwise, we will later infer it from the value
            int size = context.size == null ? 0 : context.size.GetConstantValue(Context.Outer);

            if (context.expression() != null)
            {
                value = new ExpressionVisitor(Context, size).Visit(context.expression());

                if (size == 0)
                    size = value.BitSize;
            }
            else if (size == 0)
            {
                Context.Errors.AddError("You must specify a local's size or initialize it", context.Span(), true);
            }

            var localInfo = new LocalInfo(size, name, new SourceSpan(context.VARIABLE().Symbol));
            Context.Locals.Add(name, localInfo);

            return new DeclareLocalStatement(context.Span(), localInfo, value);
        }

        public override Statement VisitTask_print([NotNull] LogicScriptParser.Task_printContext context)
        {
            if (context.expression() != null)
            {
                var value = new ExpressionVisitor(Context).Visit(context.expression());

                return new ShowTaskStatement(context.Span(), value);
            }
            else if (context.TEXT() != null)
            {
                return new PrintTaskStatement(context.Span(), context.TEXT().GetText().Trim('"'));
            }

            throw new ParseException("Invalid print value", context.Span());
        }

        public override Statement VisitTask_update([NotNull] LogicScriptParser.Task_updateContext context)
        {
            return new UpdateTaskStatement(context.Span());
        }

        public override Statement VisitStmt_break([NotNull] LogicScriptParser.Stmt_breakContext context)
        {
            return new BreakStatement(context.Span());
        }
    }
}
