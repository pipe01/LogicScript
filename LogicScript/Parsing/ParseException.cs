using System;

namespace LogicScript.Parsing
{
    public class ParseException : Exception
    {
        public SourceLocation Location { get; }

        public ParseException(string message, SourceLocation location) : base(message)
        {
            this.Location = location;
        }

        public ParseException(string message, SourceLocation location, Exception innerException) : base(message, innerException)
        {
            this.Location = location;
        }
    }
}
