using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures.Statements;
using System.Linq;

namespace LogicScript.Parsing.Visitors
{
    class StatementVisitor : LogicScriptBaseVisitor<Statement>
    {
        private readonly Script Script;

        public StatementVisitor(Script script)
        {
            this.Script = script;
        }

        public override Statement VisitBlock([NotNull] LogicScriptParser.BlockContext context)
        {
            return new BlockStatement(context.Loc(), context.statement().Select(Visit).ToArray());
        }

        public override Statement VisitAssign_statement([NotNull] LogicScriptParser.Assign_statementContext context)
        {
            var @ref = new ReferenceVisitor(Script).Visit(context.reference());

            if (!@ref.IsWritable)
                throw new ParseException("The left side of an assignment must be writable", context.reference().Loc());

            var value = new ExpressionVisitor(Script).Visit(context.expression());

            return new AssignStatement(context.Loc(), @ref, value);
        }

        public override Statement VisitIf_statement([NotNull] LogicScriptParser.If_statementContext context)
        {
            return VisitIfBody(context.if_body());
        }

        private IfStatement VisitIfBody(LogicScriptParser.If_bodyContext context)
        {
            var cond = new ExpressionVisitor(Script).Visit(context.expression());
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
    }
}
