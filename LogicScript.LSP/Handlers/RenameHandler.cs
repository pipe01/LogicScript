using LogicScript.Parsing.Structures;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.LSP.Handlers
{
    class RenameHandler : RenameHandlerBase
    {
        private readonly Workspace Workspace;

        public RenameHandler(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        protected override RenameRegistrationOptions CreateRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            var port = Workspace.GetPortAt(request.TextDocument.Uri, request.Position.ToLocation());

            if (port == null)
                return Task.FromResult(null as WorkspaceEdit);

            var newText = port is LocalInfo ? "$" + request.NewName : request.NewName;
            var refs = Workspace.FindReferencesTo(request.TextDocument.Uri, port);

            var edits = refs.Select(o => new TextEdit
            {
                NewText = newText,
                Range = o.Span.ToRange()
            }).Prepend(new()
            {
                NewText = newText,
                Range = port.Span.ToRange()
            });

            return Task.FromResult<WorkspaceEdit?>(new WorkspaceEdit
            {
                Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                {
                    { request.TextDocument.Uri, edits }
                }
            });
        }
    }
}
