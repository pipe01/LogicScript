using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
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
            var target = workspace.GetNodeAt(request.TextDocument.Uri, request.Position);

            if (target == null)
                return new();

            var highlights = new List<DocumentHighlight>();

            if (target is IPortInfo portInfo)
            {
                HighlightPortInfo(portInfo);
            }
            else if (target is Reference reference)
            {
                HighlightPortInfo(reference.Port);
            }
            else if (target is FunctionBlock function)
            {
                HighlightFunction(function);
            }
            else if (target is FunctionCallExpression functionCall)
            {
                HighlightFunction(functionCall.Function);
            }

            return highlights;

            void HighlightPortInfo(IPortInfo portInfo)
            {
                highlights.Add(new()
                {
                    Kind = DocumentHighlightKind.Write,
                    Range = portInfo.Span.ToRange()
                });

                foreach (var node in workspace.VisitAll(request.TextDocument.Uri))
                {
                    if (node is AssignStatement assign && assign.Reference.Port.Equals(portInfo))
                    {
                        highlights.Add(new()
                        {
                            Kind = DocumentHighlightKind.Write,
                            Range = assign.Reference is PortReference portRef ? portRef.PortSpan.ToRange() : assign.Reference.Span.ToRange()
                        });
                    }
                    else if (node is ReferenceExpression refExpr && refExpr.Reference.Port.Equals(portInfo))
                    {
                        highlights.Add(new()
                        {
                            Kind = DocumentHighlightKind.Read,
                            Range = refExpr.Reference is PortReference portRef ? portRef.PortSpan.ToRange() : refExpr.Reference.Span.ToRange()
                        });
                    }
                }
            }

            void HighlightFunction(FunctionBlock function)
            {
                highlights.Add(new()
                {
                    Kind = DocumentHighlightKind.Write,
                    Range = function.NameSpan.ToRange()
                });
                highlights.AddRange(
                    workspace.VisitAll(request.TextDocument.Uri)
                        .OfType<FunctionCallExpression>()
                        .Where(call => call.Function.ID == function.ID)
                        .Select(call => new DocumentHighlight()
                        {
                            Kind = DocumentHighlightKind.Read,
                            Range = call.NameSpan.ToRange()
                        })
                );
            }
        }
    }
}
