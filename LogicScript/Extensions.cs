using Antlr4.Runtime;
using LogicScript.Parsing;

namespace LogicScript
{
    internal static class Extensions
    {
        public static SourceSpan Span(this ParserRuleContext context)
            => new(context);
        public static SourceSpan Span(this IToken token)
            => new(token);
    }
}
