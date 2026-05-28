using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThrottleDebounce;

namespace LogicScript.DX.LSP.Handlers
{
    class DocumentHandler(ILanguageServerFacade server, Workspace workspace) : IDidChangeTextDocumentHandler, IDidOpenTextDocumentHandler, IDidCloseTextDocumentHandler
    {
        private readonly ILanguageServerFacade Server = server;
        private readonly Workspace Workspace = workspace;

        private readonly Dictionary<DocumentUri, RateLimitedAction<string>> SendDiagnosticsThrottled = [];

        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
                SyncKind = TextDocumentSyncKind.Full
            };
        }

        TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            ThrottlerFor(request.TextDocument.Uri).Invoke(request.TextDocument.Text);
            return Unit.Task;
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            ThrottlerFor(request.TextDocument.Uri).Invoke(request.ContentChanges.First().Text);
            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (SendDiagnosticsThrottled.TryGetValue(request.TextDocument.Uri, out var throttler))
            {
                throttler.Dispose();
                SendDiagnosticsThrottled.Remove(request.TextDocument.Uri);
            }

            return Unit.Task;
        }

        private void SendDiagnostics(string source, DocumentUri uri)
        {
            Debug.WriteLine($"Updated script at {uri}");

            var diag = Workspace.LoadScript(uri, source, out var script);

            Server.Client.SendNotification(new PublishDiagnosticsParams
            {
                Diagnostics = new(diag ?? []),
                Uri = uri
            });

            if (script != null)
            {
                Server.SendNotification("logicscript/foundTests", new
                {
                    uri,
                    tests = script.TestCases.Select(t => new
                    {
                        id = HashCode.Combine(t.Index, t.Name).ToString(),
                        name = t.Name ?? $"Case {t.Index}",
                        range = t.Span.ToRange(),
                    }),
                });
            }
        }

        private RateLimitedAction<string> ThrottlerFor(DocumentUri uri)
        {
            if (!SendDiagnosticsThrottled.TryGetValue(uri, out var throttler))
                SendDiagnosticsThrottled[uri] = throttler = Throttler.Throttle<string>(source => SendDiagnostics(source, uri), TimeSpan.FromSeconds(0.5));

            return throttler;
        }
    }
}
