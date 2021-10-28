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
            return new BlockStatement(context.Loc(), context.statement().Select(Visit).ToArray());
        }

        public override Statement VisitAssignRegular([NotNull] LogicScriptParser.AssignRegularContext context)
        {
            var @ref = new ReferenceVisitor(Context).Visit(context.reference());

            if (!@ref.IsWritable)
                throw new ParseException("The left side of an assignment must be writable", context.reference().Loc());

            var value = new ExpressionVisitor(Context, @ref.BitSize).Visit(context.expression());

            return new AssignStatement(context.Loc(), @ref, value);
        }

        public override Statement VisitAssignTruncate([NotNull] LogicScriptParser.AssignTruncateContext context)
        {
            var @ref = new ReferenceVisitor(Context).Visit(context.reference());

            if (!@ref.IsWritable)
                throw new ParseException("The left side of an assignment must be writable", context.reference().Loc());

            var value = new ExpressionVisitor(Context).Visit(context.expression());

            return new AssignStatement(context.Loc(), @ref, new TruncateExpression(context.Loc(), value, @ref.BitSize));
        }

        public override Statement VisitIf_statement([NotNull] LogicScriptParser.If_statementContext context)
        {
            return VisitIfBody(context.if_body());
        }

        private IfStatement VisitIfBody(LogicScriptParser.If_bodyContext context)
        {
            var cond = new ExpressionVisitor(Context).Visit(context.expression());
            var body = Visit(context.block());
            Statement? @else = null;

            if (context.else_statement() != null)
            {
                @else = Visit(context.else_statement().block());
            }
            else if (context.elseif_statement() != null)
            {
                @else = VisitIfBody(context.elseif_statement().if_body());
            }

            return new IfStatement(context.Loc(), cond, body, @else);
        }

        public override Statement VisitVardecl_statement([NotNull] LogicScriptParser.Vardecl_statementContext context)
        {
            var name = context.IDENT().GetText();

            if (Context.DoesIdentifierExist(name))
                throw new ParseException($"Identifier '{name}' already exists", new SourceLocation(context.IDENT().Symbol));

            Expression? value = null;

            // If the variable has a bit size marker, we will use that size. Otherwise, we will later infer it from the value
            int size = context.BIT_SIZE() != null ? context.BIT_SIZE().ParseBitSize() : 0;

            if (context.expression() != null)
            {
                value = new ExpressionVisitor(Context, size).Visit(context.expression());

                if (size == 0)
                    size = value.BitSize;
            }
            else if (size == 0)
            {
                throw new ParseException("You must specify a local's size or initialize it", context.Loc());
            }

            Context.Locals.Add(name, new LocalInfo(size));

            return new DeclareLocalStatement(context.Loc(), name, size, value);
        }

        public override Statement VisitPrint_task([NotNull] LogicScriptParser.Print_taskContext context)
        {
            if (context.expression() != null)
            {
                var value = new ExpressionVisitor(Context).Visit(context.expression());

                return new ShowTaskStatement(context.Loc(), value, false);
            }
            else if (context.TEXT() != null)
            {
                return new PrintTaskStatement(context.Loc(), context.TEXT().GetText().Trim('"'));
            }

            throw new ParseException("Invalid print value", context.Loc());
        }

        public override Statement VisitPrintbin_task([NotNull] LogicScriptParser.Printbin_taskContext context)
        {
            var value = new ExpressionVisitor(Context).Visit(context.expression());

            return new ShowTaskStatement(context.Loc(), value, true);
        }
    }
}
