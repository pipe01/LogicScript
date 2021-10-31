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
                var line = item.Location.Line - 1;
                var col = item.Location.Column - 1;

                yield return new()
                {
                    Message = item.Message,
                    Severity = item.Severity switch
                    {
                        Severity.Error => DiagnosticSeverity.Error,
                        Severity.Warning => DiagnosticSeverity.Warning,
                        _ => DiagnosticSeverity.Error
                    },
                    Range = new(line, col, line, col + 1)
                };
            }
        }
    }
}
