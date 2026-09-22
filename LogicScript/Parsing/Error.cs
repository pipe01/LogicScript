using LogicScript.Parsing.Structures;

namespace LogicScript.Parsing
{
    public enum Severity
    {
        Warning,
        Error
    }

    public class Error
    {
        public int Code { get; }
        public string Message { get; }
        public SourceSpan Span { get; }
        public Severity Severity { get; }

        internal ICodeNode? Node { get; }
        internal bool IsANTLR { get; }

        internal Error(int code, string message, SourceSpan span, Severity severity, bool isANTLR)
        {
            this.Code = code;
            this.Message = message;
            this.Span = span;
            this.Severity = severity;
            this.IsANTLR = isANTLR;
        }
        internal Error(int code, string message, ICodeNode node, Severity severity, bool isANTLR) : this(code, message, node.Span, severity, isANTLR)
        {
            this.Node = node;
            this.IsANTLR = isANTLR;
        }

        public override string ToString() => $"{Message} at {Span.Start}";
    }
}
