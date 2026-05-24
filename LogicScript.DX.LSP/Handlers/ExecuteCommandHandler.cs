using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LogicScript.DX.LSP
{
    class ExecuteCommandHandler(Workspace workspace, ILanguageServerFacade server, IServerWorkDoneManager workDoneManager) : ExecuteCommandHandlerBase
    {
        public const string RunTestCommand = "logicscript.runtest";
        public const string RunAllTestsInFileCommand = "logicscript.runtestsfile";

        protected override ExecuteCommandRegistrationOptions CreateRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                Commands = new([RunTestCommand, RunAllTestsInFileCommand]),
                WorkDoneProgress = true
            };
        }

        public override async Task<Unit> Handle(ExecuteCommandParams request, CancellationToken cancellationToken)
        {
            switch (request.Command)
            {
                case RunTestCommand:
                    {
                        string documentUri = request.Arguments?[0].ToObject<string>() ?? throw new ArgumentException("Missing document URI");
                        int testCaseIndex = request.Arguments?[1].ToObject<int>() ?? throw new ArgumentException("Missing test case index");

                        await RunTestsAsync(documentUri, [testCaseIndex], cancellationToken);
                    }
                    break;

                case RunAllTestsInFileCommand:
                    {
                        string documentUri = request.Arguments?[0].ToObject<string>() ?? throw new ArgumentException("Missing document URI");

                        await RunTestsAsync(documentUri, null, cancellationToken);
                    }
                    break;

                default:
                    throw new ArgumentException("Unknown command");
            }

            return Unit.Value;
        }

        private async Task RunTestsAsync(string documentUri, int[]? caseIndices, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(DocumentUri.Parse(documentUri), out var script))
                throw new ArgumentException("Invalid document URI");

            var progressToken = new ProgressToken(Random.Shared.Next());

            using var workDone = await workDoneManager.Create(new()
            {
                Title = "Running tests...",
            }, cancellationToken: cancellationToken);

            var cases = caseIndices == null ? script.TestCases : script.TestCases.Where(c => caseIndices.Contains(c.Index));

            int successCount = 0, failCount = 0;
            foreach (var testCase in cases)
            {
                var result = await testCase.Run(script, null);
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;

                    server.SendNotification("logicscript/logTestOutput", $"Test \"{testCase.Name ?? testCase.Index.ToString()}\": " + result.GetFailureString());
                }
            }

            if (failCount > 0)
            {
                server.Window.ShowMessage(new()
                {
                    Message = $"{failCount} test{(failCount == 1 ? "" : "s")} failed. Check output log for details",
                    Type = MessageType.Error
                });
            }
            else
            {
                server.Window.ShowMessage(new()
                {
                    Message = $"Ran {successCount} test{(successCount == 1 ? "" : "s")} successfully",
                    Type = MessageType.Info
                });
            }
        }
    }
}