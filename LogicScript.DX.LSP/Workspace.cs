using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using LogicScript.Testing;
using LogicScript.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogicScript.DX.LSP
{
    internal class Workspace
    {
        private readonly Dictionary<DocumentUri, Script> Scripts = [];
        private readonly Mutex ScriptsLock = new();

        public IReadOnlyList<Diagnostic>? LoadScript(DocumentUri uri, string text)
        {
            ScriptsLock.WaitOne();
            try
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
            finally
            {
                ScriptsLock.ReleaseMutex();
            }
        }

        public Script? ParsePartial(DocumentUri uri, SourceLocation until)
        {
            if (!TryGetScript(uri, out var origScript))
                return null;

            var source = new StringBuilder();
            var reader = new StringReader(origScript.Source);

            string? line;
            int lineIndex = 1;
            while (lineIndex <= until.Line && (line = reader.ReadLine()) != null)
            {
                if (lineIndex == until.Line && line.Length > until.Column - 1)
                {
                    line = line[..(until.Column - 1)];
                }

                source.AppendLine(line);

                lineIndex++;
            }

            var (script, _) = Script.Parse(source.ToString(), uri.ToString());

            return script;
        }

        public bool TryGetScript(DocumentUri uri, [MaybeNullWhen(false)] out Script script)
        {
            ScriptsLock.WaitOne();
            try
            {
                return Scripts.TryGetValue(uri, out script);
            }
            finally
            {
                ScriptsLock.ReleaseMutex();
            }
        }

        public IEnumerable<ICodeNode> VisitAll(DocumentUri uri)
        {
            if (TryGetScript(uri, out var script))
                return script.Blocks.SelectMany(Visit);

            return [];

            static IEnumerable<ICodeNode> Visit(ICodeNode node)
            {
                return node.GetChildren().SelectMany(Visit).Prepend(node);
            }
        }

        public IReadOnlyList<ICodeNode> FindReferencesTo(DocumentUri uri, IPortInfo port)
        {
            var refs = new List<ICodeNode>();

            foreach (var node in VisitAll(uri))
            {
                if (node is ReferenceExpression refExpr && refExpr.Reference.Port.Equals(port))
                    refs.Add(refExpr);
                else if (node is AssignStatement assign && assign.Reference.Port.Equals(port))
                    refs.Add(assign.Reference);
                else if (node is PrintTaskStatement print && port is LocalInfo local)
                    refs.AddRange(print.String.Interpolations.Where(i => i.Local.Equals(local)).Cast<ICodeNode>());
            }

            return refs;
        }

        public IPortInfo? GetPortAt(DocumentUri uri, SourceLocation location)
        {
            return GetNodeAt(uri, location) switch
            {
                Reference r => r.Port,
                IPortInfo p => p,
                PrintStringFormat.Interpolation interp => interp.Local,
                PortValue portValue when TryGetScript(uri, out var script) && script.TryGetPort(portValue.Name, portValue.Ports, out var port) => port,
                _ => null
            };
        }

        public ICodeNode? GetNodeAt(DocumentUri uri, SourceLocation location, Type[]? types = null)
        {
            if (!TryGetScript(uri, out var script))
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
