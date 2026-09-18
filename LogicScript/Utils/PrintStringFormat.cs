using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Utils
{
    public readonly struct PrintStringFormat(SourceSpan span, string text, IReadOnlyCollection<PrintStringFormat.Part> parts) : ICodeNode
    {
        public enum NumberFormat
        {
            Decimal,
            Hexadecimal,
            Binary,
        }

        public abstract record Part;

        public sealed record PartLiteral(string String) : Part;

        public sealed record PartInterpolate(SourceSpan Span, LocalInfo LocalInfo, NumberFormat Format) : Part, ICodeNode
        {
            public IEnumerable<ICodeNode> GetChildren()
            {
                yield return LocalInfo;
            }
        }

        public string Text { get; } = text;
        public IReadOnlyCollection<Part> Parts { get; } = parts;

        public SourceSpan Span => span;

        internal static PrintStringFormat Parse(SourceSpan span, string format)
            => Parse(span, format, _ => throw new InvalidOperationException("Can't access locals"));

        internal static PrintStringFormat Parse(SourceSpan span, string format, Func<string, LocalInfo> fetchLocal)
        {
            var str = new StringBuilder();
            var parts = new List<Part>();

            int removed = 0;

            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];

                if (c == '$')
                {
                    if (str.Length > 0)
                    {
                        parts.Add(new PartLiteral(str.ToString()));
                        str.Clear();
                    }

                    var match = Regex.Match(format[i..], @"(\$[a-zA-Z_][a-zA-Z0-9_]*)(:(?<base>b|x))?");

                    if (match.Success)
                    {
                        var local = fetchLocal(match.Groups[1].Value);
                        var fmtStr = match.Groups["base"].Success ? match.Groups["base"].Value : null;
                        var fmt = fmtStr switch
                        {
                            "b" => NumberFormat.Binary,
                            "x" => NumberFormat.Hexadecimal,
                            _ => NumberFormat.Decimal,
                        };

                        var interpSpan = new SourceSpan(span.Start.FileName, span.Start.Line, span.Start.Column + i + 1, span.Start.Line, span.Start.Column + i + 1 + match.Length);
                        parts.Add(new PartInterpolate(interpSpan, local, fmt));

                        removed += match.Length;
                        i += match.Length - 1;
                    }
                    else
                    {
                        str.Append(c);
                    }
                }
                else
                {
                    str.Append(c);
                }
            }

            if (str.Length > 0)
                parts.Add(new PartLiteral(str.ToString()));

            return new(span, str.ToString(), parts);
        }

        public IEnumerable<ICodeNode> GetChildren() => Parts.OfType<PartInterpolate>();
    }
}