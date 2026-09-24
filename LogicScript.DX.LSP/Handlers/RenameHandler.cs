using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class RenameHandler(Workspace workspace) : RenameHandlerBase
    {
        private readonly Workspace Workspace = workspace;

        protected override RenameRegistrationOptions CreateRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override async Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            if (!Workspace.TryGetDefinition(request.Position.ToLocation(request.TextDocument.Uri), out var definition))
                return null;

            var newText = definition is LocalInfo ? "$" + request.NewName : request.NewName;
            var refs = Workspace.FindReferencesTo(request.TextDocument.Uri, definition).Prepend((definition, false));

            var edits = refs.Select(r => new TextEdit
            {
                NewText = newText,
                Range = r.Item1 is IHasNameSpan withName ? withName.NameSpan.ToRange() : r.Item1.Span.ToRange()
            });

            return new()
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { request.TextDocument.Uri, edits }
                }
            };
        }
    }
}
