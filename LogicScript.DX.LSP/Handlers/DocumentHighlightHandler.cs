using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.DX.LSP.Handlers
{
    class DocumentHighlightHandler(Workspace workspace) : DocumentHighlightHandlerBase
    {
        protected override DocumentHighlightRegistrationOptions CreateRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override async Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
        {
            var port = workspace.GetPortAt(request.TextDocument.Uri, request.Position.ToLocation(request.TextDocument.Uri));

            if (port == null)
                return new();

            var highlights = new List<DocumentHighlight>()
            {
                new()
                {
                    Kind = DocumentHighlightKind.Write,
                    Range = port.Span.ToRange()
                }
            };

            foreach (var node in workspace.VisitAll(request.TextDocument.Uri))
            {
                if (node is AssignStatement assign && assign.Reference.Port.Equals(port))
                {
                    highlights.Add(new()
                    {
                        Kind = DocumentHighlightKind.Write,
                        Range = assign.Reference is PortReference portRef ? portRef.PortSpan.ToRange() : assign.Reference.Span.ToRange()
                    });
                }
                else if (node is ReferenceExpression refExpr && refExpr.Reference.Port.Equals(port))
                {
                    highlights.Add(new()
                    {
                        Kind = DocumentHighlightKind.Read,
                        Range = refExpr.Reference is PortReference portRef ? portRef.PortSpan.ToRange() : refExpr.Reference.Span.ToRange()
                    });
                }
            }

            return highlights;
        }
    }
}
