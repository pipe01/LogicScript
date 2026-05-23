using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.DX.LSP.Handlers
{
    class InlayHintsHandler(Workspace workspace) : InlayHintsHandlerBase
    {
        protected override InlayHintRegistrationOptions CreateRegistrationOptions(InlayHintClientCapabilities capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
                ResolveProvider = false,
            };
        }

        public override async Task<InlayHint> Handle(InlayHint request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(request.TextDocument.Uri, out var script))
                return new();

            var hints = new List<InlayHint>();

            foreach (var node in script.VisitAll())
            {
                if (node is DeclareLocalStatement stmt && !stmt.HasExplicitSize)
                {
                    var nameEnd = stmt.Local.Span.End.ToPosition();
                    var hint = $"'{stmt.Local.BitSize}";

                    hints.Add(new()
                    {
                        Position = nameEnd,
                        Label = new(hint),
                        Kind = InlayHintKind.Type,
                        Tooltip = "Inferred size from initializer",
                        TextEdits = new([
                            new() {
                                NewText = $"'{stmt.Local.BitSize}",
                                Range = new(nameEnd, nameEnd)
                            },
                        ])
                    });
                }
            }

            return new(hints);
        }
    }
}
