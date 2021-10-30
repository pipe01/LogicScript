namespace LogicScript.Parsing
{
    internal enum Severity
    {
        Warning,
        Error
    }

    internal class Error
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
    }
}
