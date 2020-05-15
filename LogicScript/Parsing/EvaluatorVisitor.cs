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
            else if (statement is ForStatement forStmt)
            {
                foreach (var item in forStmt.Body)
                {
                    Visit(item);
                }
            }
            else if (statement is ExpressionStatement exprStmt)
            {
                exprStmt.Expression = Evaluate(exprStmt.Expression);
            }

            Expression Evaluate(Expression expr)
            {
                if (expr is NumberLiteralExpression || expr is VariableAccessExpression || expr is SlotExpression || (expr is OperatorExpression op && op.Operator == Operator.Assign))
                    return expr;

                // This is kind of a hack, however it's the easiest way to precompute values
                // The logic runner will successfully compute any constant values with a null machine,
                // as it won't require it if the values are constant. Non-constant values will query
                // the machine for inputs or memory, and will throw an NRE.

                try
                {
                    //var ctx = new LogicRunner.CaseContext(null, null);
                    //var value = LogicRunner.GetValue(ref ctx, expr);

                    //Debug.WriteLine($"Evaluated '{expr}' to '{value}'");

                    //return new NumberLiteralExpression(value, expr.Location);
                    return null;
                }
                catch
                {
                    switch (expr)
                    {
                        case FunctionCallExpression f:
                            for (int i = 0; i < f.Arguments.Count; i++)
                            {
                                f.Arguments[i] = Evaluate(f.Arguments[i]);
                            }
                            break;
                        case IndexerExpression i:
                            i.Start = Evaluate(i.Start);
                            i.End = Evaluate(i.End);
                            i.Operand = Evaluate(i.Operand);
                            break;
                        case ListExpression l:
                            for (int i = 0; i < l.Expressions.Length; i++)
                            {
                                l.Expressions[i] = Evaluate(l.Expressions[i]);
                            }
                            break;
                        case OperatorExpression o:
                            o.Left = Evaluate(o.Left);
                            o.Right = Evaluate(o.Right);
                            break;
                        case UnaryOperatorExpression u:
                            u.Operand = Evaluate(u.Operand);
                            break;
                    }

                    return expr;
                }
            }
        }
    }
}
