using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace LogicScript.DX.LSP.Handlers
{
    class InlayHintsHandler(Workspace workspace, ILanguageServerFacade server) : InlayHintsHandlerBase
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

            var config = await server.GetConfigurationAsync(cancellationToken);
            var showSizeAnnotations = config.GetConfiguration(["inlayHints", "showSizeAnnotations"], true);
            var showImplicitTruncations = config.GetConfiguration(["inlayHints", "showImplicitTruncations"], true);

            var hints = new List<InlayHint>();
            var hintedLocals = new HashSet<LocalInfo>();

            foreach (var node in script.VisitAll())
            {
                if (showSizeAnnotations && node is DeclareLocalStatement stmt && !stmt.HasExplicitSize && !hintedLocals.Contains(stmt.Local))
                {
                    hintedLocals.Add(stmt.Local);

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

                if (showImplicitTruncations && node is TruncateExpression truncate && truncate.IsImplicit)
                {
                    var start = truncate.Span.Start.ToPosition();
                    var end = truncate.Span.End.ToPosition();

                    TextEdit[] textEdits = [
                        new()
                        {
                            NewText = "(",
                            Range = new(start, start),
                        },
                        new()
                        {
                            NewText = $")'{truncate.BitSize}",
                            Range = new(end, end),
                        }
                    ];

                    hints.Add(new()
                    {
                        Position = truncate.Span.Start.ToPosition(),
                        Label = new("("),
                        Kind = InlayHintKind.Type,
                        TextEdits = textEdits,
                    });
                    hints.Add(new()
                    {
                        Position = truncate.Span.End.ToPosition(),
                        Label = new($")'{truncate.BitSize}"),
                        Kind = InlayHintKind.Type,
                        TextEdits = textEdits,
                    });
                }
            }

            return new(hints);
        }
    }
}
