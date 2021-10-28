using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LogicScript.Parsing;

namespace LogicScript
{
    internal static class Extensions
    {
        public static bool ContainsDecimalDigits(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] > '1')
                    return true;
            }

            return false;
        }

        public static SourceLocation Loc(this ParserRuleContext context)
            => new SourceLocation(context.Start);

        public static int ParseBitSize(this ITerminalNode node)
            => int.Parse(node.GetText().TrimStart('\''));
    }
}
