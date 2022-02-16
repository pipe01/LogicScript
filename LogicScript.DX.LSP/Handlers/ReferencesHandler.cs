using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class ReferencesHandler : ReferencesHandlerBase
    {
        private readonly Workspace Workspace;

        public ReferencesHandler(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            var port = Workspace.GetPortAt(request.TextDocument.Uri, request.Position.ToLocation());

            if (port == null)
                return Task.FromResult(LocationContainer.From(Array.Empty<Location>()));

            var refs = Workspace.FindReferencesTo(request.TextDocument.Uri, port);

            return Task.FromResult(LocationContainer.From(refs.Select(o => new Location
            {
                Range = o.Span.ToRange(),
                Uri = request.TextDocument.Uri
            })));
        }
    }
}
