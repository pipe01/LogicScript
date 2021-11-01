using Antlr4.Runtime.Tree;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    internal static class Extensions
    {
        public static int GetConstantValue(this IParseTree tree, ScriptContext context)
            => (int)new ExpressionVisitor(new BlockContext(context, true)).Visit(tree).GetConstantValue().Number;

        public static BitsValue GetConstantValue(this Expression expr)
            => new Visitor().Visit(expr);
    }
}
