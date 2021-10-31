﻿using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

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
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position);
            var lines = new List<string>();
            int size;
            SourceSpan span;

            switch (node)
            {
                case AssignStatement assign when assign.Reference is PortReference portRef:
                    AddPortReference(portRef);
                    span = assign.Span;
                    break;

                case ReferenceExpression refExpr when refExpr.Reference is PortReference portRef:
                    AddPortReference(portRef);
                    span = refExpr.Span;
                    break;

                case Expression expr:
                    size = expr.BitSize;
                    span = expr.Span;
                    break;

                case DeclareLocalStatement local:
                    size = local.Size;
                    span = local.Span;
                    break;

                default:
                    return Task.FromResult(null as Hover);
            }

            // C# sucks
            void AddPortReference(PortReference portRef)
            {
                if (portRef.Port.BitSize == 1)
                    lines.Add($"### {portRef.Target} index {portRef.Port.StartIndex}");
                else
                    lines.Add($"### {portRef.Target} index {portRef.Port.StartIndex} to {portRef.Port.StartIndex + portRef.Port.BitSize - 1}");

                size = portRef.BitSize;
            }

            if (size != 0)
                lines.Add($"Size: `{size}` bits");

            return Task.FromResult<Hover?>(new Hover
            {
                Contents = new MarkedStringsOrMarkupContent(new MarkupContent
                {
                    Kind = MarkupKind.Markdown,
                    Value = string.Join("\n---\n", lines)
                }),
                Range = span.GetRange()
            });
        }
    }

    class DefinitionHandler : DefinitionHandlerBase
    {
        private readonly Workspace Workspace;

        public DefinitionHandler(Workspace workspace)
        {
            this.Workspace = workspace;
        }

        protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector
            };
        }

        public override Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var node = Workspace.GetNodeAt(request.TextDocument.Uri, request.Position);

            Range range;

            switch (node)
            {
                case ReferenceExpression refExpr:
                    range = refExpr.Reference.Declaration.Span.GetRange();
                    break;

                case AssignStatement assign:
                    range = assign.Reference.Declaration.Span.GetRange();
                    break;

                default:
                    return Task.FromResult(new LocationOrLocationLinks());
            }

            return Task.FromResult(new LocationOrLocationLinks(new LocationOrLocationLink(new Location
            {
                Uri = request.TextDocument.Uri,
                Range = range
            })));
        }
    }
}
