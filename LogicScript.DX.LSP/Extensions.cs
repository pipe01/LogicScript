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

        public static SourceLocation ToLocation(this Position pos, DocumentUri uri)
            => pos.ToLocation(uri.ToString());
        public static SourceLocation ToLocation(this Position pos, string fileName)
            => new(fileName, pos.Line + 1, pos.Character + 1);
    }
}
