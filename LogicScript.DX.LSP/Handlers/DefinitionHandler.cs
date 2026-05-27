using LogicScript.Parsing.Structures;
using LogicScript.Testing;
using LogicScript.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LogicScript.DX.LSP.Handlers
{
    class DefinitionHandler(Workspace workspace) : DefinitionHandlerBase
    {
        private readonly Workspace Workspace = workspace;

        protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override async Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var port = Workspace.GetPortAt(request.TextDocument.Uri, request.Position.ToLocation(request.TextDocument.Uri));

            if (port == null)
                return new();

            return new(new LocationOrLocationLink(new Location
            {
                Uri = request.TextDocument.Uri,
                Range = port.Span.ToRange()
            }));
        }
    }
}
