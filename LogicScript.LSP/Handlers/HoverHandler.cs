using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.LSP.Handlers
{
    class HoverHandler : HoverHandlerBase
    {
        private readonly Workspace Workspace;

        public HoverHandler(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position);
            var lines = new List<string>();
            int size;
            SourceSpan span;

            switch (node)
            {
                case AssignStatement assign when assign.Reference is PortReference portRef:
                    AddPortReference(portRef);
                    span = assign.Span;
                    break;

                case ReferenceExpression refExpr when refExpr.Reference is PortReference portRef:
                    AddPortReference(portRef);
                    span = refExpr.Span;
                    break;

                case Expression expr:
                    size = expr.BitSize;
                    span = expr.Span;
                    break;

                case DeclareLocalStatement local:
                    size = local.Local.BitSize;
                    span = local.Span;
                    break;

                default:
                    return Task.FromResult(null as Hover);
            }

            // C# sucks
            void AddPortReference(PortReference portRef)
            {
                if (portRef.Port.BitSize == 1)
                    lines.Add($"### {portRef.PortInfo.Target} index {portRef.PortInfo.StartIndex}");
                else
                    lines.Add($"### {portRef.PortInfo.Target} index {portRef.PortInfo.StartIndex} to {portRef.PortInfo.StartIndex + portRef.Port.BitSize - 1}");

                size = portRef.BitSize;
            }

            if (size != 0)
                lines.Add($"Size: `{size}` bits");

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
    }
}
