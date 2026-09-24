using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class ReferencesHandler(Workspace workspace) : ReferencesHandlerBase
    {
        private readonly Workspace Workspace = workspace;

        protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override async Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position);

            if (node == null)
                return new();

            var refs = Workspace.FindReferencesTo(request.TextDocument.Uri, node);

            return new(refs.Select(o => new Location
            {
                Range = o.Node.Span.ToRange(),
                Uri = request.TextDocument.Uri
            }));
        }
    }
}
