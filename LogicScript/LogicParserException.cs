using System;

namespace LogicScript
{
    public class LogicParserException : Exception
    {
        public LogicParserException() : base()
        {
        }

        public LogicParserException(string message) : base(message)
        {
        }

        public LogicParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
