using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
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

            void HighlightPortInfo(IPortInfo portInfo, ICodeNode? skip = null)
            {
                highlights.Add(new()
                {
                    Kind = DocumentHighlightKind.Write,
                    Range = portInfo.Span.ToRange()
                });
                highlights.AddRange(
                    workspace.FindReferencesTo(request.TextDocument.Uri, portInfo)
                        .Where(n => skip == null || n.Node != skip)
                        .Select(r => new DocumentHighlight()
                        {
                            Kind = r.IsWrite ? DocumentHighlightKind.Write : DocumentHighlightKind.Read,
                            Range = r.Node is IHasNameSpan hasName ? hasName.NameSpan.ToRange() : r.Node.Span.ToRange()
                        })
                );
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
