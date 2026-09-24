using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;

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
            if (Workspace.TryGetDefinition(request.Position.ToLocation(request.TextDocument.Uri), out var definition))
            {
                return new(new LocationOrLocationLink(new Location
                {
                    Uri = request.TextDocument.Uri,
                    Range = definition is IHasNameSpan hasName ? hasName.NameSpan.ToRange() : definition.Span.ToRange()
                }));
            }

            return new();
        }
    }
}
