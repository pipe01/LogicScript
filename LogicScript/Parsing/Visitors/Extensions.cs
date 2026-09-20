using Antlr4.Runtime.Tree;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    internal static class Extensions
    {
        public static BitsValue GetConstantValue(this IParseTree tree, ScriptContext context)
            => tree.GetConstantValue(context, out _);
        public static BitsValue GetConstantValue(this IParseTree tree, ScriptContext context, out Expression expr)
        {
            expr = new ExpressionVisitor(new BlockContext(context, null, true)).Visit(tree);
            return expr.GetConstantValue();
        }

        public static BitsValue GetConstantValue(this Expression expr)
            => Interpreter.Visit(expr);
    }
}
