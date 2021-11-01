using LogicScript.LSP.Handlers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LogicScript.LSP
{
    class Program
    {
        public static readonly DocumentSelector Selector = new(new DocumentFilter
        {
            Language = "logicscript"
        });

        static async Task Main(string[] args)
        {
            Debugger.Launch();

            var server = LanguageServer.Create(opts =>
            {
                opts.WithServices(o =>
                {
                    o.AddSingleton(new Workspace());
                });

                opts.WithOutput(Console.OpenStandardOutput());
                opts.WithInput(Console.OpenStandardInput());

                opts.WithHandler<DocumentHandler>();
                opts.WithHandler<HoverHandler>();
                opts.WithHandler<DefinitionHandler>();
                opts.WithHandler<RenameHandler>();
                opts.WithHandler<ReferencesHandler>();
            });

            await server.Initialize(CancellationToken.None);
            await server.WaitForExit;
        }
    }
}
