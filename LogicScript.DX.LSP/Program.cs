using LogicScript.DX.LSP.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.DX.LSP
{
    class Program
    {
        public static readonly TextDocumentSelector Selector = TextDocumentSelector.ForLanguage("logicscript");

        static async Task Main(string[] args)
        {
            var server = LanguageServer.Create(opts => opts
                .WithServices(o =>
                {
                    o.AddSingleton(new Workspace());
                })

                .WithOutput(Console.OpenStandardOutput())
                .WithInput(Console.OpenStandardInput())

                .ConfigureLogging(logging =>
                {
                    logging.AddLanguageProtocolLogging();
                })

                .WithHandler<DocumentHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<DefinitionHandler>()
                .WithHandler<RenameHandler>()
                .WithHandler<ReferencesHandler>()
            );

            await server.Initialize(CancellationToken.None);
            await server.WaitForExit;
        }
    }
}
