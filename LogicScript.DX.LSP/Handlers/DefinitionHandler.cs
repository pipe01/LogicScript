using LogicScript.Parsing.Structures;
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

        public override Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position);

            Range range;

            switch (node)
            {
                case Reference @ref:
                    range = @ref.Port.Span.ToRange();
                    break;

                case PrintStringFormat.Interpolation interp:
                    range = interp.Local.Span.ToRange();
                    break;

                default:
                    return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
            }

            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(new LocationOrLocationLink(new Location
            {
                Uri = request.TextDocument.Uri,
                Range = range
            })));
        }
    }
}
