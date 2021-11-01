using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.LSP
{
    internal class Workspace
    {
        private IDictionary<DocumentUri, Script> Scripts = new Dictionary<DocumentUri, Script>();

        public IReadOnlyList<Diagnostic>? LoadScript(DocumentUri uri, string text)
        {
            var (script, errors) = Script.Parse(text);

            if (script == null)
            {
                return errors.Select(item =>
                {
                    var start = (Line: item.Span.Start.Line - 1, Col: item.Span.Start.Column - 1);
                    var end = (Line: item.Span.End.Line - 1, Col: item.Span.End.Column - 1);

                    return new Diagnostic()
                    {
                        Message = item.Message,
                        Severity = item.Severity switch
                        {
                            Severity.Error => DiagnosticSeverity.Error,
                            Severity.Warning => DiagnosticSeverity.Warning,
                            _ => DiagnosticSeverity.Error
                        },
                        Range = new(start.Line, start.Col, end.Line, end.Col)
                    };
                }).ToArray();
            }

            Scripts[uri] = script;
            return null;
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
            var editedNode = GetNodeAt(uri, location);

            if (editedNode is Reference reference)
            {
                return reference.Port;
            }
            else if (editedNode is IPortInfo portInfo)
            {
                return portInfo;
            }

            return null;
        }

        public ICodeNode? GetNodeAt(DocumentUri uri, SourceLocation location)
        {
            if (!Scripts.TryGetValue(uri, out var script))
                return null;

            return script.GetNodeAt(location);
        }
        public ICodeNode? GetNodeAt(DocumentUri uri, Position position)
            => GetNodeAt(uri, new SourceLocation(position.Line + 1, position.Character + 1));
    }
}
