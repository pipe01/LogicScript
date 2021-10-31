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
        public string Message { get; }
        public SourceLocation Location { get; }
        public Severity Severity { get; }

        internal ICodeNode? Node { get; }
        internal bool IsANTLR { get; }

        internal Error(string message, SourceLocation location, Severity severity, bool isANTLR)
        {
            this.Message = message;
            this.Location = location;
            this.Severity = severity;
            this.IsANTLR = isANTLR;
        }
        internal Error(string message, ICodeNode node, Severity severity, bool isANTLR) : this(message, node.Location, severity, isANTLR)
        {
            this.Node = node;
            this.IsANTLR = isANTLR;
        }

        public override string ToString() => $"{Message} at {Location}";
    }
}
