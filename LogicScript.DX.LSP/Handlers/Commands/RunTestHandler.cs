using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Compiling;
using LogicScript.DX.DAP;
using LogicScript.DX.LSP.Debugging;
using LogicScript.Testing;
using LogicScript.Testing.Results;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LogicScript.DX.LSP.Commands
{
    class RunTestHandler(Workspace workspace, ILanguageServerFacade server) : ExecuteCommandHandlerBase<object>
    {
        public const string RunTestCommand = "logicscript/runTest";

        protected override ExecuteCommandRegistrationOptions CreateRegistrationOptions(ExecuteCommandCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                Commands = new([RunTestCommand]),
                WorkDoneProgress = true
            };
        }

        public override async Task<object> Handle(ExecuteCommandParams<object> request, CancellationToken cancellationToken)
        {
            var args = request.Arguments?.ToArray() ?? [];

            var scriptUri = args[0].ToObject<string>() ?? throw new ArgumentException("Missing script URI");
            var testCaseID = args[1].ToObject<string>() ?? throw new ArgumentException("Missing test ID");
            var debug = args[2].ToObject<bool>();

            if (!workspace.TryGetScript(scriptUri, out var script))
                throw new ArgumentException("Script not found");

            var testCase = script.TestCases.FirstOrDefault(t => HashCode.Combine(t.Index, t.Name).ToString() == testCaseID);
            if (testCase == default)
                throw new ArgumentException("Test not found");

            var statementLimit = await server.GetConfigurationAsync(["test", "statementLimit"], -1, cancellationToken);

            CaseResult result = new SuccessStepResult(testCase, []);
            await RunTestAsync(scriptUri, testCase, statementLimit, DebugSession.Current?.Debugger, workspace, cancellationToken);

            return new
            {
                success = result.Success,
                scriptOutput = result.PrintedLines,
                result = Testing.FormatResult(result, testCase, false),
            };
        }

        private static async Task<CaseResult> RunTestAsync(DocumentUri documentUri, TestCase testCase, int statementLimit, LogicScriptDebugger? debugger, Workspace workspace, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(documentUri, out var script))
                throw new ArgumentException("Script not loaded: " + documentUri);

            debugger?.LoadedScript(script);

            // TODO: implement statement limit
            var compiledScript = Compiler.Compile(script, debugger != null);

            return await testCase.Run(compiledScript, script, debugger, cancellationToken);
        }
    }
}
