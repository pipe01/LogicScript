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

        internal Error(string message, SourceLocation location, Severity severity)
        {
            this.Message = message;
            this.Location = location;
            this.Severity = severity;
        }
        internal Error(string message, ICodeNode node, Severity severity) : this(message, node.Location, severity)
        {
            this.Node = node;
        }

        public override string ToString() => $"{Message} at {Location}";
    }
}
