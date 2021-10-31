using LogicScript.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;

namespace LogicScript.LSP
{
    public static class ScriptDiagnostics
    {
        public static IEnumerable<Diagnostic> ForText(string script)
        {
            var (_, errors) = Script.Parse(script);

            foreach (var item in errors)
            {
                var start = (Line: item.Span.Start.Line - 1, Col: item.Span.Start.Column - 1);
                var end = (Line: item.Span.End.Line - 1, Col: item.Span.End.Column - 1);

                yield return new()
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
            }
        }
    }
}
