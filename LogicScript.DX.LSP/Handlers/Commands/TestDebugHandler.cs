using System;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.DX.LSP.Debugging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LogicScript.DX.LSP.Commands
{
    class StartTestDebugHandler : ExecuteCommandHandlerBase<object>
    {
        public const string StartTestDebugCommand = "logicscript/startTestDebug";

        protected override ExecuteCommandRegistrationOptions CreateRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                Commands = new([StartTestDebugCommand]),
            };
        }

        public override async Task<object> Handle(ExecuteCommandParams<object> request, CancellationToken cancellationToken)
        {
            switch (request.Command)
            {
                case StartTestDebugCommand:
                    DebugSession.Current?.Stop();

                    var endpoint = DebugSession.Start();

                    return new
                    {
                        host = endpoint.Address.ToString(),
                        port = endpoint.Port,
                    };
            }

            throw new ArgumentException("Unknown command");
        }
    }
}
