using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LogicScript.DX.LSP
{
    class CompletionRequestHandler(Workspace workspace) : CompletionHandlerBase
    {
        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = Program.Selector,
            };
        }

        public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            if (!workspace.TryGetScript(request.TextDocument.Uri, out var script))
                return new();

            var completions = new List<CompletionItem>();
            var location = request.Position.ToLocation(request.TextDocument.Uri.ToString());

            bool inBlock = false;

            foreach (var node in script.VisitAll())
            {
                if (!node.Span.Contains(location))
                    continue;

                if (node is Block)
                {
                    inBlock = true;
                }
                else if (node is BlockStatement block)
                {
                    foreach (var local in block.Locals)
                    {
                        completions.Add(new()
                        {
                            Label = local.Name,
                            Kind = CompletionItemKind.Variable,
                        });
                    }
                }
            }

            var keywords = new List<string>();
            if (inBlock)
            {
                keywords.AddRange(["if", "else", "for", "from", "to", "end", "break", "while", "@print", "@queueUpdate", "local"]);

                foreach (var item in script.Inputs.Concat(script.Outputs).Concat(script.Registers))
                {
                    completions.Add(new()
                    {
                        Label = item.Key,
                        Kind = CompletionItemKind.Variable,
                        LabelDetails = new()
                        {
                            Description = item.Value.Target.ToString()
                        }
                    });
                }

                foreach (var item in script.Constants)
                {
                    completions.Add(new()
                    {
                        Label = item.Key,
                        Kind = CompletionItemKind.Constant,
                        Detail = "Value: " + item.Value,
                    });
                }
            }
            else
            {
                keywords.AddRange(["const", "input", "output", "reg", "when", "startup", "assign", "@test"]);
            }

            foreach (var item in keywords)
            {
                completions.Add(new()
                {
                    Label = item,
                    Kind = CompletionItemKind.Keyword,
                });
            }

            return new(completions);
        }

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}