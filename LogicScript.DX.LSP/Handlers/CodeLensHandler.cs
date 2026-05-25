using LogicScript.Testing;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP.Handlers
{
    class CodeLensHandler(Workspace workspace) : CodeLensHandlerBase
    {
        protected override CodeLensRegistrationOptions CreateRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override async Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
        {
            var codeLens = new List<CodeLens>();

            if (workspace.TryGetScript(request.TextDocument.Uri, out var script))
            {
                foreach (var node in script.VisitAll())
                {
                    if (node is TestCase testCase)
                    {
                        codeLens.Add(new()
                        {
                            Range = new(testCase.Span.Start.Line - 1, 0, testCase.Span.Start.Line - 1, 0),
                            Command = new()
                            {
                                Name = ExecuteCommandHandler.RunTestCommand,
                                Arguments = [request.TextDocument.Uri.ToString(), testCase.Index],
                                Title = "Run test"
                            }
                        });

                        codeLens.Add(new()
                        {
                            Range = new(testCase.Span.Start.Line - 1, 0, testCase.Span.Start.Line - 1, 0),
                            Command = new()
                            {
                                Name = "logicscript.tests.debugFile",
                                Arguments = [request.TextDocument.Uri.ToString(), testCase.Index],
                                Title = "Debug test"
                            }
                        });
                    }
                }
            }

            return new(codeLens);
        }
    }
}
