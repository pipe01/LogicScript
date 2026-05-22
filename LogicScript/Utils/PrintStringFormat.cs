using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Utils
{
    public readonly struct PrintStringFormat(SourceSpan span, string text, IReadOnlyCollection<PrintStringFormat.Interpolation> interpolations) : ICodeNode
    {
        public readonly struct Interpolation(SourceSpan span, int position, LocalInfo local, string? format) : ICodeNode
        {
            public int Position { get; } = position;
            internal LocalInfo Local { get; } = local;
            public string? Format { get; } = format;

            public SourceSpan Span => span;

            public IEnumerable<ICodeNode> GetChildren()
            {
                yield return Local;
            }
        }

        public string Text { get; } = text;
        public IReadOnlyCollection<Interpolation> Interpolations { get; } = interpolations;

        public SourceSpan Span => span;

        public string ToFormattable()
        {
            var str = new StringBuilder(Text);

            int offset = 0;

            int i = 0;
            foreach (var intp in Interpolations)
            {
                var format = intp.Format != null ? $":{intp.Format}" : string.Empty;
                var interpString = $"{{{i++}{format}}}";

                str.Insert(intp.Position + offset, interpString);

                offset += interpString.Length;
            }

            return str.ToString();
        }

        internal static PrintStringFormat Parse(SourceSpan span, string format)
            => Parse(span, format, _ => throw new InvalidOperationException("Can't access locals"));

        internal static PrintStringFormat Parse(SourceSpan span, string format, Func<string, LocalInfo> fetchLocal)
        {
            var str = new StringBuilder();
            var interpolations = new List<Interpolation>();

            int removed = 0;

            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];

                if (c == '$')
                {
                    var match = Regex.Match(format[i..], @"(\$[a-zA-Z_][a-zA-Z0-9_]*)(:(?<base>b|x))?");

                    if (match.Success)
                    {
                        var local = fetchLocal(match.Groups[1].Value);
                        var fmt = match.Groups["base"].Success ? match.Groups["base"].Value : null;

                        var interpSpan = new SourceSpan(span.Start.FileName, span.Start.Line, span.Start.Column + i + 1, span.Start.Line, span.Start.Column + i + 1 + match.Length);
                        interpolations.Add(new(interpSpan, i - removed, local, fmt));

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

            return new(span, str.ToString(), interpolations);
        }

        public IEnumerable<ICodeNode> GetChildren() => Interpolations.Cast<ICodeNode>();
    }
}