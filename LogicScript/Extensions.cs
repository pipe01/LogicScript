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
            => new(context.Start);

        public static SourceSpan Span(this ParserRuleContext context)
            => new(context);

        public static int ParseBitSize(this ITerminalNode node)
        {
            var size = int.Parse(node.GetText().TrimStart('\''));
            if (size == 0)
                throw new ParseException("Bit size must be greater than 0", new SourceSpan(node.Symbol));

            return size;
        }
    }
}
