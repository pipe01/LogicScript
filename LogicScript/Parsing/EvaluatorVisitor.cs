using LogicScript.Parsing.Structures;
using System.Diagnostics;

namespace LogicScript.Parsing
{
    internal static class EvaluatorVisitor
    {
        public static void Visit(Script script)
        {
            foreach (var item in script.TopLevelNodes)
            {
                if (item is Case c)
                {
                    foreach (var stmt in c.Statements)
                    {
                        Visit(stmt);
                    }
                }
            }
        }

        public static void Visit(Statement statement)
        {
            if (statement is IfStatement ifStmt)
            {
                ifStmt.Condition = Evaluate(ifStmt.Condition);

                foreach (var item in ifStmt.Body)
                {
                    Visit(item);
                }

                foreach (var item in ifStmt.Else)
                {
                    Visit(item);
                }
            }
            //else if (statement is ExpressionStatement exprStmt && exprStmt.Expression is )
            //{
            //    assignStmt.LeftSide = Evaluate(assignStmt.LeftSide);
            //    assignStmt.RightSide = Evaluate(assignStmt.RightSide);
            //}

            Expression Evaluate(Expression expr)
            {
                // This is kind of a hack, however it's the easiest way to precompute values
                // The logic runner will successfully compute any constant values with a null machine,
                // as it won't require it if the values are constant. Non-constant values will query
                // the machine for inputs or memory, and will throw an NRE.

                try
                {
                    var value = new LogicRunner().GetValue(new LogicRunner.CaseContext(), expr);

                    Debug.WriteLine($"Evaluated '{expr}' to '{value}'");

                    return new NumberLiteralExpression(expr.Location, value.Number, value.Length);
                }
                catch
                {
                    return expr;
                }
            }
        }
    }
}
