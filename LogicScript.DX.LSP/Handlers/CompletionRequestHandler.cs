using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
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
            var script = workspace.ParsePartial(request.TextDocument.Uri, request.Position.ToLocation(request.TextDocument.Uri));

            if (script == null)
                return new();

            var completions = new List<CompletionItem>();
            var location = request.Position.ToLocation(request.TextDocument.Uri);

            bool addTopLevelKeywords = true,
                addBlockLevelKeywords = false,
                addWritables = false,
                addReadables = false,
                inLoop = false,
                inIf = false;

            foreach (var node in script.VisitAll())
            {
                if (node is PlaceholderExpression)
                {
                    addReadables = true;
                    addTopLevelKeywords = false;
                    addBlockLevelKeywords = false;
                    break;
                }
                if (node is PlaceholderAssignBlock)
                {
                    addWritables = true;
                    addTopLevelKeywords = false;
                    addBlockLevelKeywords = false;
                    break;
                }

                if (!node.Span.Contains(location))
                    continue;

                if (node is Block)
                {
                    addTopLevelKeywords = false;
                    addBlockLevelKeywords = true;
                }
                else if (node is BlockStatement block)
                {
                    addWritables = true;
                }
                else if (node is ForStatement or WhileStatement)
                {
                    inLoop = true;
                }
                else if (node is IfStatement)
                {
                    inIf = true;
                }
            }

            var locals = script.VisitAll().OfType<BlockStatement>().Where(s => s.Span.Contains(location)).SelectMany(b => b.Locals);

            if (addWritables)
            {
                AddPorts(script.Outputs, true);
            }
            if (addReadables)
            {
                foreach (var item in script.Constants)
                {
                    completions.Add(new()
                    {
                        Label = item.Key,
                        Kind = CompletionItemKind.Constant,
                        Detail = "Value: " + item.Value,
                    });
                }

                foreach (var item in new string[] { "len", "allOnes" })
                {
                    completions.Add(new()
                    {
                        Label = item,
                        Kind = CompletionItemKind.Keyword,
                        InsertText = item + "($0)",
                        InsertTextFormat = InsertTextFormat.Snippet,
                    });
                }

                AddPorts(script.Inputs, false);
            }
            if (addReadables || addWritables)
            {
                foreach (var local in locals)
                {
                    completions.Add(new()
                    {
                        Label = local.Name,
                        Kind = CompletionItemKind.Variable,
                        LabelDetails = new()
                        {
                            Description = $"'{local.BitSize}"
                        }
                    });
                }

                AddPorts(script.Registers, addWritables);
            }

            var keywords = new List<string>();
            if (addBlockLevelKeywords)
            {
                keywords.AddRange(["if", "for", "while", "local", "from", "to", "end", "@print", "@queueUpdate"]);
                if (inLoop) keywords.Add("break");
                if (inIf) keywords.Add("else");

                completions.Add(new()
                {
                    Label = "if",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "if ${1:condition}\n\t$0\nend"
                });
                completions.Add(new()
                {
                    Label = "for",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "for $${1:i} to ${2}\n\t$0\nend"
                });
                completions.Add(new()
                {
                    Label = "while",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "while ${1:condition}\n\t$0\nend"
                });
                completions.Add(new()
                {
                    Label = "localinit",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "local $$1 = $0"
                });
                completions.Add(new()
                {
                    Label = "localsize",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "local $$1'$0"
                });
            }
            if (addTopLevelKeywords)
            {
                keywords.AddRange(["const", "input", "output", "reg", "when", "startup", "assign", "@test"]);

                completions.Add(new()
                {
                    Label = "test",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "@test (\n\t$0\n)\n"
                });
                completions.Add(new()
                {
                    Label = "always",
                    Kind = CompletionItemKind.Snippet,
                    InsertTextFormat = InsertTextFormat.Snippet,
                    InsertText = "when *\n\t$0\nend"
                });
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

            void AddPorts(IEnumerable<KeyValuePair<string, MachinePortInfo>> ports, bool writable)
            {
                foreach (var item in ports)
                {
                    completions.Add(new()
                    {
                        Label = item.Key,
                        Kind = CompletionItemKind.Variable,
                        InsertText = writable ? item.Key + " = " : null,
                        LabelDetails = new()
                        {
                            Description = $"{item.Value.Target.ToString().ToLowerInvariant()}'{item.Value.BitSize}"
                        }
                    });
                }
            }
        }

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}