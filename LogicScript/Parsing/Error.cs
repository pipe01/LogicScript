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

        public Error(string message, SourceLocation location, Severity severity)
        {
            this.Message = message;
            this.Location = location;
            this.Severity = severity;
        }

        public override string ToString() => $"{Message} at {Location}";
    }
}
