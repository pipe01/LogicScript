using LogicScript.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.LSP
{
    public static class Extensions
    {
        public static Range GetRange(this SourceSpan span)
            => new(span.Start.Line - 1, span.Start.Column - 1, span.End.Line - 1, span.End.Column - 1);
    }
}
