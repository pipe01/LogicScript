using LogicScript.Interpreting;

namespace LogicScript.Parsing.Visitors
{
    internal static class Extensions
    {
        public static int GetConstantValue(this LogicScriptParser.AtomContext atom, ScriptContext context)
            => (int)new Visitor().Visit(new ExpressionVisitor(new BlockContext(context, true)).Visit(atom)).Number;
    }
}
