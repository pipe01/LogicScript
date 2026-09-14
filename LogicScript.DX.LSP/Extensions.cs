using LogicScript.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.DX.LSP
{
    public static class Extensions
    {
        public static Range ToRange(this SourceSpan span)
            => new(span.Start.ToPosition(), span.End.ToPosition());

        public static Position ToPosition(this SourceLocation loc)
            => new(loc.Line - 1, loc.Column - 1);

        public static SourceLocation ToLocation(this Position pos, DocumentUri uri, int characterOffset = 0)
            => pos.ToLocation(uri.ToString(), characterOffset);
        public static SourceLocation ToLocation(this Position pos, string fileName, int characterOffset = 0)
            => new(fileName, pos.Line + 1, pos.Character + 1 + characterOffset);
    }
}
