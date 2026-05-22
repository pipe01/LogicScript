using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.DX.LSP
{
    internal class Workspace
    {
        private readonly Dictionary<DocumentUri, Script> Scripts = [];

        public IReadOnlyList<Diagnostic>? LoadScript(DocumentUri uri, string text)
        {
            var (script, errors) = Script.Parse(text, uri.ToString());

            if (script != null)
                Scripts[uri] = script;

            return errors.Select(item =>
            {
                return new Diagnostic()
                {
                    Message = item.Message,
                    Severity = item.Severity switch
                    {
                        Severity.Error => DiagnosticSeverity.Error,
                        Severity.Warning => DiagnosticSeverity.Warning,
                        _ => DiagnosticSeverity.Error
                    },
                    Range = item.Span.ToRange()
                };
            }).ToArray();
        }

        public void VisitAll(DocumentUri uri, Action<ICodeNode> action)
        {
            if (!Scripts.TryGetValue(uri, out var script))
                return;

            foreach (var block in script.Blocks)
            {
                Visit(block);
            }

            void Visit(ICodeNode node)
            {
                foreach (var child in node.GetChildren())
                {
                    Visit(child);
                }

                action(node);
            }
        }

        public IReadOnlyList<ICodeNode> FindReferencesTo(DocumentUri uri, IPortInfo port)
        {
            var refs = new List<ICodeNode>();

            VisitAll(uri, node =>
            {
                if (node is ReferenceExpression refExpr && refExpr.Reference.Port.Equals(port))
                    refs.Add(refExpr);
                else if (node is AssignStatement assign && assign.Reference.Port.Equals(port))
                    refs.Add(assign.Reference);
            });

            return refs;
        }

        public IPortInfo? GetPortAt(DocumentUri uri, SourceLocation location)
        {
            return GetNodeAt(uri, location) switch
            {
                Reference r => r.Port,
                IPortInfo p => p,
                _ => null
            };
        }

        public ICodeNode? GetNodeAt(DocumentUri uri, SourceLocation location, Type[]? types = null)
        {
            if (!Scripts.TryGetValue(uri, out var script))
                return null;

            if (types == null || types.Contains(typeof(PortInfo)))
            {
                foreach (var port in script.Inputs.Values.Concat(script.Outputs.Values).Concat(script.Registers.Values))
                {
                    if (port.Span.Contains(location))
                        return port;
                }
            }

            return script.GetNodeAt(location, types);
        }
        public ICodeNode? GetNodeAt(DocumentUri uri, Position position, Type[]? types = null)
            => GetNodeAt(uri, new SourceLocation(uri.ToString(), position.Line + 1, position.Character + 1), types);
    }
}
