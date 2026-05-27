using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Parsing.Visitors;
using LogicScript.Testing;
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
                typeof(MachinePortInfo),
                typeof(Reference),
                typeof(Expression),
                typeof(DeclareLocalStatement),
                typeof(PortValues),
            ]);
            var lines = new List<string>();
            int size;
            SourceSpan span;

            switch (node)
            {
                case MachinePortInfo port:
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

                    if (expr.IsConstant)
                    {
                        try
                        {
                            lines.Add($"Constant: `{expr.GetConstantValue()}`");
                        }
                        catch { }
                    }

                    break;

                case DeclareLocalStatement local:
                    size = local.Local.BitSize;
                    span = local.Local.Span;
                    break;

                case PortValue portValue:
                    size = portValue.Value.Length;
                    span = portValue.Span;
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
                    Value = string.Join("\n\n", lines)
                }),
                Range = span.ToRange()
            });
        }

        private static string GetPortDescription(MachinePortInfo port)
        {
            return port.BitSize == 1
                ? $"**{port.Target} index {port.StartIndex}**"
                : $"**{port.Target} index {port.StartIndex} to {port.StartIndex + port.BitSize - 1}**";
        }
    }
}
