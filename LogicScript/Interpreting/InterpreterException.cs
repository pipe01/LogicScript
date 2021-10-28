using LogicScript.Parsing;
using System;

namespace LogicScript.Interpreting
{
    public class InterpreterException : Exception
    {
        public SourceLocation Location { get; }

        public InterpreterException(string message, SourceLocation location) : base(message)
        {
            this.Location = location;
        }

        public InterpreterException(string message, SourceLocation location, Exception innerException) : base(message, innerException)
        {
            this.Location = location;
        }
    }
}
