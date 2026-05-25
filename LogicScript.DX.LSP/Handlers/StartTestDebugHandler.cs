using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.DX.LSP.Debugging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.WorkDone;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LogicScript.DX.LSP
{
    class StartTestDebugHandler(Workspace workspace, ILanguageServerFacade server, IServerWorkDoneManager workDoneManager) : ExecuteCommandHandlerBase<JObject>
    {
        public const string StartTestDebugCommand = "logicscript/startTestDebug";

        protected override ExecuteCommandRegistrationOptions CreateRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                Commands = new([StartTestDebugCommand]),
                WorkDoneProgress = true
            };
        }

        public override async Task<JObject> Handle(ExecuteCommandParams<JObject> request, CancellationToken cancellationToken)
        {
            var args = request.Arguments?.ToArray() ?? [];

            var scriptUri = args[0].ToObject<string>() ?? throw new ArgumentException("Missing script URI");
            var testCaseIndices = args.Length >= 2 ? args[1].ToObject<int[]>() : null;

            DebugSession.Current?.Stop();

            var endpoint = DebugSession.Start();

            _ = Task.Run(async () =>
            {
                await DebugSession.Current!.Debugger.WaitForAttachedAsync();

                try
                {
                    await Runner.RunTestsAsync(scriptUri, testCaseIndices, -1, DebugSession.Current.Debugger, workspace, server, workDoneManager, DebugSession.Current.CancellationToken);
                }
                finally
                {
                    DebugSession.Current.Stop();
                }
            });

            return new()
            {
                ["host"] = endpoint.Address.ToString(),
                ["port"] = endpoint.Port,
            };
        }
    }
}
