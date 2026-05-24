using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Testing.Results;
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
            var args = request.Arguments?.ToArray() ?? [];

            switch (request.Command)
            {
                case RunTestCommand:
                    {
                        string documentUri = args[0].ToObject<string>() ?? throw new ArgumentException("Missing document URI");
                        int testCaseIndex = args[1].ToObject<int>();
                        int statementLimit = args.Length >= 3 ? args[2].ToObject<int>() : await RequestStatementLimitAsync(cancellationToken);

                        await RunTestsAsync(documentUri, [testCaseIndex], statementLimit, cancellationToken);
                    }
                    break;

                case RunAllTestsInFileCommand:
                    {
                        string documentUri = args[0].ToObject<string>() ?? throw new ArgumentException("Missing document URI");
                        int statementLimit = args.Length >= 2 ? args[1].ToObject<int>() : await RequestStatementLimitAsync(cancellationToken);

                        await RunTestsAsync(documentUri, null, statementLimit, cancellationToken);
                    }
                    break;

                default:
                    throw new ArgumentException("Unknown command");
            }

            return Unit.Value;
        }

        private async Task RunTestsAsync(string documentUri, int[]? caseIndices, int statementLimit, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(DocumentUri.Parse(documentUri), out var script))
                throw new ArgumentException("Invalid document URI");

            var progressToken = new ProgressToken(Random.Shared.Next());

            server.SendNotification("logicscript/clearTestOutput");

            using var workDone = await workDoneManager.Create(new()
            {
                Title = "Running tests...",
            }, cancellationToken: cancellationToken);

            var cases = caseIndices == null ? script.TestCases : script.TestCases.Where(c => caseIndices.Contains(c.Index));

            int successCount = 0, failCount = 0;
            foreach (var testCase in cases)
            {
                var result = await testCase.Run(script, null, statementLimit);
                if (result.Success)
                {
                    successCount++;
                    continue;
                }
                failCount++;

                var message = new StringBuilder();
                message.AppendLine($"* At {testCase.Span.Start.FileName}:{testCase.Span.Start}");
                message.AppendLine($"** Test \"{testCase.Name ?? testCase.Index.ToString()}\" failed:");

                switch (result)
                {
                    case FailedStepCaseResult failedStep:
                        message.AppendLine(failedStep.GetFailureString());
                        break;

                    case LimitReachedCaseResult limitReached:
                        message.AppendLine("Statement limit reached");
                        message.AppendLine("Check your code for any infinite loops or try raising the statement limit in the extension's settings.");
                        break;
                }

                server.SendNotification("logicscript/logTestOutput", message.ToString());
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

        private async Task<int> RequestStatementLimitAsync(CancellationToken cancellationToken)
        {
            var config = await server.Workspace.RequestConfiguration(new()
            {
                Items = new([
                    new() { Section = "logicscript" }
                ])
            }, cancellationToken: cancellationToken);

            return config.FirstOrDefault()?["test"]?["statementLimit"]?.ToObject<int>() ?? -1;
        }
    }
}