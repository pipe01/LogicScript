using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class HoverHandler(Workspace workspace) : HoverHandlerBase
    {
        private readonly Workspace Workspace = workspace;

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position, [
                typeof(PrintStringFormat.Interpolation),
                typeof(PortInfo),
                typeof(Reference),
                typeof(Expression),
                typeof(DeclareLocalStatement),
            ]);
            var lines = new List<string>();
            int size;
            SourceSpan span;

            switch (node)
            {
                case PortInfo port:
                    lines.Add(GetPortDescription(port));
                    size = port.BitSize;
                    span = port.Span;
                    break;

                case Reference @ref:
                    if (@ref is PortReference portRef)
                        lines.Add(GetPortDescription(portRef.PortInfo));

                    size = @ref.BitSize;
                    span = @ref.Span;
                    break;

                case PrintStringFormat.Interpolation interp:
                    size = interp.Local.BitSize;
                    span = interp.Span;
                    break;

                case Expression expr:
                    size = expr.BitSize;
                    span = expr.Span;
                    break;

                case DeclareLocalStatement local:
                    size = local.Local.BitSize;
                    span = local.Local.Span;
                    break;

                default:
                    return Task.FromResult<Hover?>(null);
            }

            if (size != 0)
                lines.Add($"Size: `{size}` bit" + (size != 1 ? "s" : ""));

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = string.Join("\n---\n", lines)
                }),
                Range = span.ToRange()
            });
        }

        private static string GetPortDescription(PortInfo port)
        {
            return port.BitSize == 1
                ? $"### {port.Target} index {port.StartIndex}"
                : $"### {port.Target} index {port.StartIndex} to {port.StartIndex + port.BitSize - 1}";
        }
    }
}
