using Antlr4.Runtime;
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
    }
}
