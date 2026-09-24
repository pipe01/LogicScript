using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

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

        public static async Task<JToken?> GetConfigurationAsync(this ILanguageServerFacade server, CancellationToken cancellationToken)
        {
            var config = await server.Workspace.RequestConfiguration(new()
            {
                Items = new([
                    new() { Section = "logicscript" }
                ])
            }, cancellationToken: cancellationToken);

            return config.FirstOrDefault();
        }

        public static async Task<T> GetConfigurationAsync<T>(this ILanguageServerFacade server, string[] path, T defaultValue, CancellationToken cancellationToken)
        {
            return (await server.GetConfigurationAsync(cancellationToken)).GetConfiguration(path, defaultValue);
        }

        public static T GetConfiguration<T>(this JToken? configItem, System.ReadOnlySpan<string> path, T defaultValue)
        {
            foreach (var part in path)
            {
                if (configItem == null)
                    return defaultValue;

                configItem = configItem[part];
            }

            if (configItem == null)
                return defaultValue;

            return configItem.ToObject<T>() ?? defaultValue;
        }
    }
}
