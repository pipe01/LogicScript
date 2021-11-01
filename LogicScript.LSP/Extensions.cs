using LogicScript.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.LSP
{
    public static class Extensions
    {
        public static Range ToRange(this SourceSpan span)
            => new(span.Start.Line - 1, span.Start.Column - 1, span.End.Line - 1, span.End.Column - 1);

        public static SourceLocation ToLocation(this Position pos)
            => new(pos.Line + 1, pos.Character + 1);
    }
}
