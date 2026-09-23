using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing
{
    public enum Severity
    {
        Warning,
        Error
    }

    public class Error(int code, string message, SourceSpan span, Severity severity, bool isANTLR, object? data = null)
    {
        public int Code { get; } = code;
        public string Message { get; } = message;
        public SourceSpan Span { get; } = span;
        public Severity Severity { get; } = severity;
        public object? Data { get; } = data;

        internal ICodeNode? Node { get; }
        internal bool IsANTLR { get; } = isANTLR;

        internal Error(int code, string message, ICodeNode node, Severity severity, bool isANTLR, object? data = null) : this(code, message, node.Span, severity, isANTLR, data)
        {
            this.Node = node;
            this.IsANTLR = isANTLR;
        }

        public override string ToString() => $"{Message} at {Span.Start}";
    }
}
