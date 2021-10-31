using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
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

        public ICodeNode? GetNodeAt(DocumentUri uri, SourceLocation location)
        {
            var script = Scripts[uri];

            return script.GetNodeAt(location);
        }
        public ICodeNode? GetNodeAt(DocumentUri uri, Position position)
            => GetNodeAt(uri, new SourceLocation(position.Line + 1, position.Character + 1));
    }
}
