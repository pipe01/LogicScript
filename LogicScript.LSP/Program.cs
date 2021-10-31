using MediatR;
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

        public DocumentHandler(ILanguageServerFacade server)
        {
            this.Server = server;
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

        public async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            SendDiagnostics(request.TextDocument.Text, request.TextDocument.Uri);
            return Unit.Value;
        }

        public async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            SendDiagnostics(request.ContentChanges.First().Text, request.TextDocument.Uri);
            return Unit.Value;
        }

        private void SendDiagnostics(string script, DocumentUri uri)
        {
            var diag = ScriptDiagnostics.ForText(script);

            Server.Client.SendNotification(new PublishDiagnosticsParams
            {
                Diagnostics = Container<Diagnostic>.From(diag),
                Uri = uri
            });
        }
    }

    class HoverHandler : HoverHandlerBase
    {
        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.PlainText,
                    Value = "Nice"
                })
            });
        }
    }
}
