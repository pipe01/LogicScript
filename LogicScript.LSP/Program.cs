using LogicScript.Parsing.Structures.Expressions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Diagnostics;
using System.Linq;
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
            });

            await server.Initialize(CancellationToken.None);
            await server.WaitForExit;
        }
    }

    class DocumentHandler : IDidChangeTextDocumentHandler, IDidOpenTextDocumentHandler
    {
        private readonly ILanguageServerFacade Server;
        private readonly Workspace Workspace;

        public DocumentHandler(ILanguageServerFacade server, Workspace workspace)
        {
            this.Server = server;
            this.Workspace = workspace;
        }

        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
                SyncKind = TextDocumentSyncKind.Full
            };
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            SendDiagnostics(request.TextDocument.Text, request.TextDocument.Uri);
            return Unit.Task;
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            SendDiagnostics(request.ContentChanges.First().Text, request.TextDocument.Uri);
            return Unit.Task;
        }

        private void SendDiagnostics(string script, DocumentUri uri)
        {
            var diag = Workspace.LoadScript(uri, script);

            Server.Client.SendNotification(new PublishDiagnosticsParams
            {
                Diagnostics = Container<Diagnostic>.From(diag ?? Array.Empty<Diagnostic>()),
                Uri = uri
            });
        }
    }

    class HoverHandler : HoverHandlerBase
    {
        private readonly Workspace Workspace;

        public HoverHandler(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, new(request.Position.Line + 1, request.Position.Character + 1));
            MarkupContent content;

            switch (node)
            {
                case Expression expr:
                    content = new MarkupContent
                    {
                        Kind = MarkupKind.Markdown,
                        Value = $"Size: `{expr.BitSize}` bits"
                    };
                    break;
                default:
                    return Task.FromResult(null as Hover);
            }

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(content)
            });
        }
    }
}
