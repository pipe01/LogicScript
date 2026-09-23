using LogicScript.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class CodeActionHandler(Workspace Workspace) : CodeActionHandlerBase
    {
        protected override CodeActionRegistrationOptions CreateRegistrationOptions(CodeActionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
                CodeActionKinds = new(CodeActionKind.QuickFix),
            };
        }

        public override async Task<CodeAction> Handle(CodeAction request, CancellationToken cancellationToken)
        {
            return new();
        }

        public override async Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken)
        {
            var ret = new List<CommandOrCodeAction>();

            if (!Workspace.TryGetScript(request.TextDocument.Uri, out var script))
                return new();

            foreach (var diag in request.Context.Diagnostics)
            {
                var error = diag.Data?.ToObject<Error>();
                if (error == null)
                    continue;

                var text = error.Span.GetText(script.Source);

                if (diag.Code == ErrorCodes.ExpressionTooLarge && error.Data != null)
                {
                    var maxSize = (long)error.Data;

                    ret.Add(new(new CodeAction()
                    {
                        Title = "Truncate value",
                        Kind = CodeActionKind.QuickFix,
                        Edit = new()
                        {
                            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                            {
                                [request.TextDocument.Uri] = [
                                    new()
                                    {
                                        Range = diag.Range,
                                        NewText = $"({text})'{maxSize}"
                                    }
                                ],
                            }
                        }
                    }));
                }
            }

            return ret;
        }
    }
}
