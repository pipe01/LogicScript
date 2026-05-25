using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Interpreting.Debugging;
using LogicScript.Testing.Results;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace LogicScript.DX.LSP
{
    internal static class Runner
    {
        public static async Task RunTestsAsync(string documentUri, int[]? caseIndices, int statementLimit, IDebugger? debugger, Workspace workspace, ILanguageServerFacade server, IServerWorkDoneManager workDoneManager, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(DocumentUri.Parse(documentUri), out var script))
                throw new ArgumentException("Script not loaded: " + documentUri);

            debugger?.LoadedScript(script);

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
                var result = await testCase.Run(script, debugger, statementLimit);
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
                        message.Append(failedStep.GetFailureString());
                        break;

                    case LimitReachedCaseResult limitReached:
                        message.AppendLine("Statement limit reached");
                        message.AppendLine("Check your code for any infinite loops or try raising the statement limit in the extension's settings.");
                        break;
                }

                if (result.PrintedLines.Count > 0)
                {
                    message.AppendLine("\nOutput:");

                    foreach (var line in result.PrintedLines)
                    {
                        message.AppendLine("  " + line);
                    }
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
    }
}