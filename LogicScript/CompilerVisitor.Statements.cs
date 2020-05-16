using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        public void Visit(IReadOnlyList<Statement> statements)
        {
            foreach (var stmt in statements)
            {
                Visit(stmt);
            }
        }

        private void Visit(Statement statement)
        {
            Generator.MarkLabel(Generator.DefineLabel(statement.ToString()));

            switch (statement)
            {
                case ExpressionStatement expr:
                    Visit(expr.Expression);
                    Generator.Pop();
                    break;

                case IfStatement ifStmt:
                    Visit(ifStmt);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(IfStatement stmt)
        {
            Visit(stmt.Condition);
            BitsValueToNumber();

            if (stmt.Else == null)
            {
                var bodyEndLabel = Generator.DefineLabel("body");

                Generator.Brfalse(bodyEndLabel);

                Visit(stmt.Body);
                Generator.MarkLabel(bodyEndLabel);
            }
            else
            {
                var mainStartLabel = Generator.DefineLabel("main");
                var mainEndLabel = Generator.DefineLabel("else");

                Generator.Brtrue(mainStartLabel);

                Visit(stmt.Else);
                Generator.Br(mainEndLabel);

                Generator.MarkLabel(mainStartLabel);
                Visit(stmt.Body);
                Generator.MarkLabel(mainEndLabel);
            }
        }
    }
}
