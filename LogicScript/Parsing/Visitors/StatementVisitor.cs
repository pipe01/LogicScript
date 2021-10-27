using Antlr4.Runtime.Misc;
using LogicScript.Parsing.Structures;
using System.Linq;

namespace LogicScript.Parsing.Visitors
{
    class StatementVisitor : LogicScriptBaseVisitor<Statement>
    {
        public override Statement VisitExpr_statement([NotNull] LogicScriptParser.Expr_statementContext context)
        {
            return new ExpressionStatement(new ExpressionVisitor().Visit(context.expression()), context.StartLocation());
        }

        public override Statement VisitBlock([NotNull] LogicScriptParser.BlockContext context)
        {
            return new BlockStatement(context.statement().Select(Visit).ToArray(), context.StartLocation());
        }
    }
}
