using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LogicScript.DX.LSP.Commands
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

                        await Runner.RunTestsAsync(documentUri, [testCaseIndex], statementLimit, null, workspace, server, workDoneManager, cancellationToken);
                    }
                    break;

                case RunAllTestsInFileCommand:
                    {
                        string documentUri = args[0].ToObject<string>() ?? throw new ArgumentException("Missing document URI");
                        int statementLimit = args.Length >= 2 ? args[1].ToObject<int>() : await RequestStatementLimitAsync(cancellationToken);

                        await Runner.RunTestsAsync(documentUri, null, statementLimit, null, workspace, server, workDoneManager, cancellationToken);
                    }
                    break;

                default:
                    throw new ArgumentException("Unknown command");
            }

            return Unit.Value;
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