using Antlr4.Runtime.Tree;
using LogicScript.Interpreting;

namespace LogicScript.Parsing.Visitors
{
    internal static class Extensions
    {
        public static int GetConstantValue(this IParseTree tree, ScriptContext context)
            => (int)new Visitor().Visit(new ExpressionVisitor(new BlockContext(context, true)).Visit(tree)).Number;
    }
}
