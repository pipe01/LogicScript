using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.DX.LSP.Handlers
{
    internal class SemanticTokensHandler(Workspace Workspace) : SemanticTokensHandlerBase
    {
        private static readonly SemanticTokensLegend Legend = new()
        {
            TokenTypes = new(SemanticTokenType.Method),
        };

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
                Full = true,
                Legend = Legend
            };
        }

        protected override async Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return new(Legend);
        }

        protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            if (!Workspace.TryGetScript(identifier.TextDocument.Uri, out var script))
                return Task.CompletedTask;

            var spans = new SortedSet<SourceSpan>();

            foreach (var node in script.VisitAll())
            {
                SourceSpan? funcNameSpan =
                    node is FunctionBlock func ? func.NameSpan :
                    node is FunctionCallExpression funcCall ? funcCall.NameSpan :
                    null;

                if (funcNameSpan != null)
                    spans.Add(funcNameSpan.Value);
            }

            foreach (var span in spans)
            {
                builder.Push(span.ToRange(), SemanticTokenType.Method, Array.Empty<SemanticTokenModifier>());
            }

            builder.Commit();

            return Task.CompletedTask;
        }
    }
}
